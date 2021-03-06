﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;
using Roslyn.Scripting;
using Roslyn.Scripting.CSharp;
using ScriptCs.Contracts;

namespace ScriptCs.Engine.Roslyn
{
    public class RoslynScriptEngine : IScriptEngine
    {
        private readonly ILog _log;
        protected readonly ScriptEngine ScriptEngine;
        private readonly IScriptHostFactory _scriptHostFactory;

        public const string SessionKey = "Session";
        private const string InvalidNamespaceError = "error CS0246";

        [Obsolete("Support for Common.Logging types was deprecated in version 0.15.0 and will soon be removed.")]
        public RoslynScriptEngine(IScriptHostFactory scriptHostFactory, Common.Logging.ILog logger)
            : this(scriptHostFactory, new CommonLoggingLogProvider(logger))
        {
        }

        public RoslynScriptEngine(IScriptHostFactory scriptHostFactory, ILogProvider logProvider)
        {
            Guard.AgainstNullArgument("logProvider", logProvider);

            ScriptEngine = new ScriptEngine();
            _scriptHostFactory = scriptHostFactory;
            _log = logProvider.ForCurrentType();
#pragma warning disable 618
            Logger = new ScriptCsLogger(_log);
#pragma warning restore 618
        }

        [Obsolete("Support for Common.Logging types was deprecated in version 0.15.0 and will soon be removed.")]
        protected Common.Logging.ILog Logger { get; private set; }

        public string BaseDirectory
        {
            get { return ScriptEngine.BaseDirectory; }
            set { ScriptEngine.BaseDirectory = value; }
        }

        public string CacheDirectory { get; set; }

        public string FileName { get; set; }

        public ScriptResult Execute(
            string code,
            string[] scriptArgs,
            AssemblyReferences references,
            IEnumerable<string> namespaces,
            ScriptPackSession scriptPackSession)
        {
            Guard.AgainstNullArgument("scriptPackSession", scriptPackSession);
            Guard.AgainstNullArgument("references", references);

            _log.Debug("Starting to create execution components");
            _log.Debug("Creating script host");

            var executionReferences = references.Union(scriptPackSession.References);

            SessionState<Session> sessionState;

            var isFirstExecution = !scriptPackSession.State.ContainsKey(SessionKey);

            if (isFirstExecution)
            {
                code = code.DefineTrace();
                var host = _scriptHostFactory.CreateScriptHost(
                    new ScriptPackManager(scriptPackSession.Contexts), scriptArgs);

                ScriptLibraryWrapper.SetHost(host);
                _log.Debug("Creating session");

                var hostType = host.GetType();
                ScriptEngine.AddReference(hostType.Assembly);
                var session = ScriptEngine.CreateSession(host, hostType);
                var allNamespaces = namespaces.Union(scriptPackSession.Namespaces).Distinct();

                foreach (var reference in executionReferences.Paths)
                {
                    _log.DebugFormat("Adding reference to {0}", reference);
                    session.AddReference(reference);
                }

                foreach (var assembly in executionReferences.Assemblies)
                {
                    _log.DebugFormat("Adding reference to {0}", assembly.FullName);
                    session.AddReference(assembly);
                }

                foreach (var @namespace in allNamespaces)
                {
                    _log.DebugFormat("Importing namespace {0}", @namespace);
                    session.ImportNamespace(@namespace);
                }

                sessionState = new SessionState<Session>
                {
                    References = executionReferences,
                    Session = session,
                    Namespaces = new HashSet<string>(allNamespaces)
                };
                
                scriptPackSession.State[SessionKey] = sessionState;
            }
            else
            {
                _log.Debug("Reusing existing session");
                sessionState = (SessionState<Session>)scriptPackSession.State[SessionKey];

                if (sessionState.References == null)
                {
                    sessionState.References = new AssemblyReferences();
                }

                if (sessionState.Namespaces == null)
                {
                    sessionState.Namespaces = new HashSet<string>();
                }

                var newReferences = executionReferences.Except(sessionState.References);

                foreach (var reference in newReferences.Paths)
                {
                    _log.DebugFormat("Adding reference to {0}", reference);
                    sessionState.Session.AddReference(reference);
                    sessionState.References = sessionState.References.Union(new[] { reference });
                }

                foreach (var assembly in newReferences.Assemblies)
                {
                    _log.DebugFormat("Adding reference to {0}", assembly.FullName);
                    sessionState.Session.AddReference(assembly);
                    sessionState.References = sessionState.References.Union(new[] { assembly });
                }

                var newNamespaces = namespaces.Except(sessionState.Namespaces);

                foreach (var @namespace in newNamespaces)
                {
                    _log.DebugFormat("Importing namespace {0}", @namespace);
                    sessionState.Session.ImportNamespace(@namespace);
                    sessionState.Namespaces.Add(@namespace);
                }
            }

            _log.Debug("Starting execution");

            var result = Execute(code, sessionState.Session);

            if (result.InvalidNamespaces.Any())
            {
                var pendingNamespacesField = sessionState.Session.GetType().GetField(
                    "pendingNamespaces",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (pendingNamespacesField != null)
                {
                    var pendingNamespacesValue = (ReadOnlyArray<string>)pendingNamespacesField
                        .GetValue(sessionState.Session);

                    //no need to check this for null as ReadOnlyArray is a value type
                    if (pendingNamespacesValue.Any())
                    {
                        var fixedNamespaces = pendingNamespacesValue.ToList();

                        foreach (var @namespace in result.InvalidNamespaces)
                        {
                            sessionState.Namespaces.Remove(@namespace);
                            fixedNamespaces.Remove(@namespace);
                        }
                        pendingNamespacesField.SetValue(
                            sessionState.Session, ReadOnlyArray<string>.CreateFrom(fixedNamespaces));
                    }
                }
            }

            _log.Debug("Finished execution");
            return result;
        }

        protected virtual ScriptResult Execute(string code, Session session)
        {
            Guard.AgainstNullArgument("session", session);

            try
            {
                var submission = session.CompileSubmission<object>(code);

                try
                {
                    return new ScriptResult(submission.Execute());
                }
                catch (AggregateException ex)
                {
                    return new ScriptResult(executionException: ex.InnerException);
                }
                catch (Exception ex)
                {
                    return new ScriptResult(executionException: ex);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.StartsWith(InvalidNamespaceError))
                {
                    var offendingNamespace = Regex.Match(ex.Message, @"\'([^']*)\'").Groups[1].Value;
                    return new ScriptResult(
                        compilationException: ex, invalidNamespaces: new string[1] { offendingNamespace });
                }

                return new ScriptResult(compilationException: ex);
            }
        }

        protected static bool IsCompleteSubmission(string code)
        {
            var options = new ParseOptions(
                CompatibilityMode.None,
                LanguageVersion.CSharp4,
                true,
                SourceCodeKind.Interactive);

            var syntaxTree = SyntaxTree.ParseText(code, options: options);

            return Syntax.IsCompleteSubmission(syntaxTree);
        }
    }
}
