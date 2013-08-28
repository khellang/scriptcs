using System;

using Moq;

using Ploeh.AutoFixture.Xunit;

using ScriptCs.Contracts;

using Should;

using Xunit.Extensions;

namespace ScriptCs.Tests
{
    public class LoadLineProcessorTests
    {
        public class TheProcessLineMethod : IDisposable
        {
            private const string EnvVarKey = "scriptcs";

            private const string EnvVarValue = "Awesomeness!";

            public TheProcessLineMethod()
            {
                Environment.SetEnvironmentVariable(EnvVarKey, EnvVarValue);
            }

            [Theory, ScriptCsAutoData]
            public void ShouldReturnTrueOnLoadLine(IScriptParser parser, LoadLineProcessor processor)
            {
                // Arrange
                const string Line = @"#load ""script.csx""";

                // Act
                var result = processor.ProcessLine(parser, new ScriptParserContext(), Line, true);

                // Assert
                result.ShouldBeTrue();
            }

            [Theory, ScriptCsAutoData]
            public void ShouldReturnFalseOtherwise(IScriptParser parser, LoadLineProcessor processor)
            {
                // Arrange
                const string Line = @"var x = new Test();";

                // Act
                var result = processor.ProcessLine(parser, new ScriptParserContext(), Line, true);

                // Assert
                result.ShouldBeFalse();
            }

            [Theory, ScriptCsAutoData]
            public void ShouldReturnTrueButNotParseFileIfAfterCode(
                [Frozen] Mock<IScriptParser> parser,
                [Frozen] Mock<IFileSystem> fileSystem,
                LoadLineProcessor processor)
            {
                // Arrange
                var context = new ScriptParserContext();

                const string RelativePath = "..\\script.csx";
                const string Line = @"#load " + RelativePath;
                const string FullPath = "C:\\script.csx";

                fileSystem.Setup(x => x.GetFullPath(RelativePath)).Returns(FullPath);
                
                // Act
                var result = processor.ProcessLine(parser.Object, context, Line, false);

                // Assert
                result.ShouldBeTrue();
                parser.Verify(x => x.ParseFile(FullPath, It.IsAny<ScriptParserContext>()), Times.Never());
            }

            [Theory, ScriptCsAutoData]
            public void ShouldParseLoadedFile(
                [Frozen] Mock<IScriptParser> parser,
                [Frozen] Mock<IFileSystem> fileSystem,
                LoadLineProcessor processor)
            {
                // Arrange
                var context = new ScriptParserContext();

                const string RelativePath = "..\\script.csx";
                const string Line = @"#load " + RelativePath;
                const string FullPath = "C:\\script.csx";

                fileSystem.Setup(x => x.GetFullPath(RelativePath)).Returns(FullPath);

                // Act
                processor.ProcessLine(parser.Object, context, Line, true);

                // Assert
                parser.Verify(x => x.ParseFile(FullPath, It.IsAny<ScriptParserContext>()));
            }

            [Theory, ScriptCsAutoData]
            public void ShouldExpandEnvironmentVariables(
                [Frozen] Mock<IFileSystem> fileSystem,
                LoadLineProcessor processor,
                IScriptParser parser)
            {
                // Arrange
                var context = new ScriptParserContext();
                var line = string.Format("#load %{0}%", EnvVarKey);

                // Act
                processor.ProcessLine(parser, context, line, true);

                // Assert
                fileSystem.Verify(x => x.GetFullPath(EnvVarValue));
            }

            public void Dispose()
            {
                Environment.SetEnvironmentVariable(EnvVarKey, null);
            }
        }
    }
}