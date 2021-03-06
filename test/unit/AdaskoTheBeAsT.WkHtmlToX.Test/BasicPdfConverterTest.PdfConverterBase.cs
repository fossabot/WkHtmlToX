using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AdaskoTheBeAsT.WkHtmlToX.Abstractions;
using AdaskoTheBeAsT.WkHtmlToX.Documents;
using AdaskoTheBeAsT.WkHtmlToX.Exceptions;
using AdaskoTheBeAsT.WkHtmlToX.Settings;
using AutoFixture;
using FluentAssertions;
using FluentAssertions.Execution;
using Moq;
using Xunit;

namespace AdaskoTheBeAsT.WkHtmlToX.Test
{
    public sealed partial class BasicPdfConverterTest
    {
        public static IEnumerable<object?[]> GetTestData()
        {
            const string htmlContent = "<html><head><title>title</title></head><body></body></html>";
            yield return new object?[]
            {
                htmlContent,
                null,
                null,
            };

            var htmlContentByteArray = Encoding.UTF8.GetBytes(htmlContent);
            yield return new object?[]
            {
                null,
                htmlContentByteArray,
                null,
            };

#pragma warning disable IDISP001 // Dispose created.
            var stream = new MemoryStream(htmlContentByteArray);
#pragma warning restore IDISP001 // Dispose created.
            yield return new object?[]
            {
                null,
                null,
                stream,
            };
        }

        [Fact]
        public void PdfModuleConstructorShouldThrowExceptionWhenNullPassed()
        {
            // Arrange
            var moduleMock = new Mock<IWkHtmlToXModuleFactory>();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

            // ReSharper disable once AssignmentIsFullyDiscarded
#pragma warning disable IDISP004 // Don't ignore created IDisposable.
            Action action = () => _ = new BasicPdfConverter(moduleMock.Object, null);
#pragma warning restore IDISP004 // Don't ignore created IDisposable.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            // Act & Assert
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GetApplySettingFuncShouldReturnGlobalApplySettingWhenIsGlobalTruePassed()
        {
            // Arrange
            var intVal1 = _fixture.Create<int>();
#pragma warning disable S1854 // Unused assignments should be removed
            var intVal2 = _fixture.Create<int>();
#pragma warning restore S1854 // Unused assignments should be removed
            var name = _fixture.Create<string>();
            var value = _fixture.Create<string>();

            int SetGlobalSetting(
                IntPtr ptr,
                string n,
                string? v) =>
                intVal1;

            int SetObjectSetting(
                IntPtr ptr,
                string n,
                string? v) =>
                intVal2;

            _module.Setup(m => m.SetGlobalSetting(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<string?>()))
                    .Returns((Func<IntPtr, string, string?, int>)SetGlobalSetting);
            _pdfModule.Setup(m => m.SetObjectSetting(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<string?>()))
                .Returns((Func<IntPtr, string, string?, int>)SetObjectSetting);

            // Act
            var resultFunc = _sut.GetApplySettingFunc(true);
            var result = resultFunc(new IntPtr(1), name, value);

            // Assert
            using (new AssertionScope())
            {
                resultFunc.Should().NotBeNull();
                result.Should().Be(intVal1);
            }
        }

        [Fact]
        public void GetApplySettingFuncShouldReturnObjectApplySettingWhenIsGlobalFalsePassed()
        {
            // Arrange
#pragma warning disable S1854 // Unused assignments should be removed
            var intVal1 = _fixture.Create<int>();
#pragma warning restore S1854 // Unused assignments should be removed
            var intVal2 = _fixture.Create<int>();
            var name = _fixture.Create<string>();
            var value = _fixture.Create<string>();

            int SetGlobalSetting(
                IntPtr ptr,
                string n,
                string? v) =>
                intVal1;

            int SetObjectSetting(
                IntPtr ptr,
                string n,
                string? v) =>
                intVal2;

            _module.Setup(m => m.SetGlobalSetting(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<string?>()))
                .Returns((Func<IntPtr, string, string?, int>)SetGlobalSetting);
            _pdfModule.Setup(m => m.SetObjectSetting(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<string?>()))
                .Returns((Func<IntPtr, string, string?, int>)SetObjectSetting);

            // Act
            var resultFunc = _sut.GetApplySettingFunc(false);
            var result = resultFunc(new IntPtr(1), name, value);

            // Assert
            using (new AssertionScope())
            {
                resultFunc.Should().NotBeNull();
                result.Should().Be(intVal2);
            }
        }

        [Fact]
        public void CreateConverterShouldThrowArgumentNullExceptionWhenNullPassed()
        {
            // Arrange
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

            // ReSharper disable once AssignmentIsFullyDiscarded
            Action action = () => _ = _sut.CreateConverter(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            // Act & Assert
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void CreateConverterShouldInvokeCreateGlobalSettings()
        {
            // Arrange
            var globalSettingsPtr = new IntPtr(_fixture.Create<int>());
            var converterPtr = new IntPtr(_fixture.Create<int>());
            _module.Setup(m =>
                    m.CreateGlobalSettings())
                .Returns(globalSettingsPtr);
            _module.Setup(m =>
                    m.CreateConverter(It.IsAny<IntPtr>()))
                .Returns(converterPtr);
            _module.Setup(
                m =>
                    m.SetGlobalSetting(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<string?>()));
            _pdfModule.Setup(
                m =>
                    m.SetObjectSetting(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<string?>()));
            var document = new HtmlToPdfDocument();

            // Act
            var result = _sut.CreateConverter(document);

            // Assert
            using (new AssertionScope())
            {
                _module.Verify(m => m.CreateGlobalSettings(), Times.Once);
                _module.Verify(
                    m =>
                        m.SetGlobalSetting(
                            It.IsAny<IntPtr>(),
                            It.IsAny<string>(),
                            It.IsAny<string?>()),
                    Times.Never);
                _pdfModule.Verify(
                    m =>
                        m.SetObjectSetting(
                            It.IsAny<IntPtr>(),
                            It.IsAny<string>(),
                            It.IsAny<string?>()),
                    Times.Never);
                result.converterPtr.Should().Be(converterPtr);
                result.globalSettingsPtr.Should().Be(globalSettingsPtr);
                result.objectSettingsPtrs.Should().NotBeNull();
                result.objectSettingsPtrs.Should().BeEmpty();
            }
        }

        [Fact]
        public void CreateConverterShouldSetGlobalSettings()
        {
            // Arrange
            var globalSettingsPtr = new IntPtr(_fixture.Create<int>());
            var converterPtr = new IntPtr(_fixture.Create<int>());
            _module.Setup(m =>
                    m.CreateGlobalSettings())
                .Returns(globalSettingsPtr);
            _module.Setup(m =>
                    m.CreateConverter(It.IsAny<IntPtr>()))
                .Returns(converterPtr);
            _module.Setup(
                m =>
                    m.SetGlobalSetting(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<string?>()));
            _pdfModule.Setup(
                m =>
                    m.SetObjectSetting(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<string?>()));
            var document = new HtmlToPdfDocument();
            var documentTitle = _fixture.Create<string>();
            document.GlobalSettings.DocumentTitle = documentTitle;

            // Act
            var result = _sut.CreateConverter(document);

            // Assert
            using (new AssertionScope())
            {
                _module.Verify(m => m.CreateGlobalSettings(), Times.Once);
                _module.Verify(
                    m =>
                        m.SetGlobalSetting(
                            It.Is<IntPtr>(v => v == globalSettingsPtr),
                            It.Is<string>(v => v == "documentTitle"),
                            It.Is<string?>(v => v == documentTitle)),
                    Times.Once);
                _pdfModule.Verify(
                    m =>
                        m.SetObjectSetting(
                            It.IsAny<IntPtr>(),
                            It.IsAny<string>(),
                            It.IsAny<string?>()),
                    Times.Never);
                result.converterPtr.Should().Be(converterPtr);
                result.globalSettingsPtr.Should().Be(globalSettingsPtr);
                result.objectSettingsPtrs.Should().NotBeNull();
                result.objectSettingsPtrs.Should().BeEmpty();
            }
        }

#pragma warning disable MA0051 // Method is too long
        [Fact]
        public void CreateConverterShouldSetObjectSettings()
        {
            // Arrange
            var globalSettingsPtr = new IntPtr(_fixture.Create<int>());
            var objectSettingsPtr = new IntPtr(_fixture.Create<int>());
            var converterPtr = new IntPtr(_fixture.Create<int>());
            _module.Setup(m =>
                    m.CreateGlobalSettings())
                .Returns(globalSettingsPtr);
            _module.Setup(m =>
                    m.CreateConverter(It.IsAny<IntPtr>()))
                .Returns(converterPtr);
            _module.Setup(
                m =>
                    m.SetGlobalSetting(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<string?>()));
            _pdfModule.Setup(m =>
                    m.CreateObjectSettings())
                .Returns(objectSettingsPtr);
            _pdfModule.Setup(
                m =>
                    m.SetObjectSetting(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<string?>()));
            var document = new HtmlToPdfDocument();
            var documentTitle = _fixture.Create<string>();
            var captionText = _fixture.Create<string>();
            document.GlobalSettings.DocumentTitle = documentTitle;
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            document.ObjectSettings.Add(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
            document.ObjectSettings.Add(
                new PdfObjectSettings
                {
                    CaptionText = captionText,
                    HtmlContent = "<html><head><title>title</title></head><body></body></html>",
                });

            // Act
            var result = _sut.CreateConverter(document);

            // Assert
            using (new AssertionScope())
            {
                _module.Verify(m => m.CreateGlobalSettings(), Times.Once);
                _module.Verify(
                    m =>
                        m.SetGlobalSetting(
                            It.Is<IntPtr>(v => v == globalSettingsPtr),
                            It.Is<string>(v => v == "documentTitle"),
                            It.Is<string?>(v => v == documentTitle)),
                    Times.Once);
                _pdfModule.Verify(m => m.CreateObjectSettings(), Times.Once);
                _pdfModule.Verify(
                    m =>
                        m.SetObjectSetting(
                            It.Is<IntPtr>(v => v == objectSettingsPtr),
                            It.Is<string>(v => v == "toc.captionText"),
                            It.Is<string?>(v => v == captionText)),
                    Times.Once);
                result.converterPtr.Should().Be(converterPtr);
                result.globalSettingsPtr.Should().Be(globalSettingsPtr);
                result.objectSettingsPtrs.Should().NotBeNull();
                result.objectSettingsPtrs.Should().HaveCount(1);
                result.objectSettingsPtrs[0].Should().Be(objectSettingsPtr);
            }
        }
#pragma warning restore MA0051 // Method is too long

        [Fact]
        public void ConvertImplShouldThrowExceptionWhenNullDocumentPassed()
        {
            // Arrange
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Action action = () => _sut.ConvertImpl(null, _ => Stream.Null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            // Act & Assert
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ConvertImplShouldThrowExceptionWhenNullCreateStreamPassed()
        {
            // Arrange
            var document = new HtmlToPdfDocument();
            document.ObjectSettings.Add(new PdfObjectSettings());
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Action action = () => _sut.ConvertImpl(document, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            // Act & Assert
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ConvertImplShouldThrowExceptionWhenObjectSettingsListEmpty()
        {
            // Arrange
            var document = new HtmlToPdfDocument();
            Action action = () => _sut.ConvertImpl(document, _ => Stream.Null);

            // Act & Assert
            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ConvertImplShouldThrowExceptionWhenModuleInitializeNotEqualOne()
        {
            // Arrange
            var document = new HtmlToPdfDocument();
            document.ObjectSettings.Add(new PdfObjectSettings());
            _module.Setup(m => m.Initialize(It.IsAny<int>()))
                .Returns(0);

            Action action = () => _sut.ConvertImpl(document, _ => Stream.Null);

            // Act & Assert
            action.Should().Throw<ArgumentException>();
        }

#pragma warning disable MA0051 // Method is too long
        [Fact]
        public void ConvertImplShouldReturnNullStreamWhenNotConverted()
        {
            // Arrange
            var globalSettingsPtr = new IntPtr(_fixture.Create<int>());
            var objectSettingsPtr = new IntPtr(_fixture.Create<int>());
            var converterPtr = new IntPtr(_fixture.Create<int>());
            _module.Setup(m => m.Initialize(It.IsAny<int>()))
                .Returns(1);
            _module.Setup(m =>
                    m.CreateGlobalSettings())
                .Returns(globalSettingsPtr);
            _module.Setup(m =>
                    m.CreateConverter(It.IsAny<IntPtr>()))
                .Returns(converterPtr);
            _module.Setup(
                m =>
                    m.SetGlobalSetting(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<string?>()));
            _module.Setup(m => m.Convert(It.IsAny<IntPtr>()))
                .Returns(false);
            _module.Setup(m => m.GetOutput(It.IsAny<IntPtr>(), It.IsAny<Func<int, Stream>>()));
            _module.Setup(m => m.DestroyGlobalSetting(It.IsAny<IntPtr>()));
            _module.Setup(m => m.DestroyConverter(It.IsAny<IntPtr>()));
            _module.Setup(m => m.Terminate());
            _pdfModule.Setup(m =>
                    m.CreateObjectSettings())
                .Returns(objectSettingsPtr);
            _pdfModule.Setup(
                m =>
                    m.SetObjectSetting(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<string?>()));
            _pdfModule.Setup(m => m.DestroyObjectSetting(It.IsAny<IntPtr>()));
            var document = new HtmlToPdfDocument();
            var documentTitle = _fixture.Create<string>();
            var captionText = _fixture.Create<string>();
            document.GlobalSettings.DocumentTitle = documentTitle;
            document.ObjectSettings.Add(
                new PdfObjectSettings
                {
                    CaptionText = captionText,
                    HtmlContent = "<html><head><title>title</title></head><body></body></html>",
                });

            // Act
            var result = _sut.ConvertImpl(document, _ => Stream.Null);

            // Assert
            using (new AssertionScope())
            {
                _module.Verify(m => m.Initialize(It.IsAny<int>()), Times.Once);
                _module.Verify(m => m.CreateGlobalSettings(), Times.Once);
                _module.Verify(
                    m =>
                        m.SetGlobalSetting(
                            It.Is<IntPtr>(v => v == globalSettingsPtr),
                            It.Is<string>(v => v == "documentTitle"),
                            It.Is<string?>(v => v == documentTitle)),
                    Times.Once);
                _pdfModule.Verify(m => m.CreateObjectSettings(), Times.Once);
                _module.Verify(m => m.GetOutput(It.IsAny<IntPtr>(), It.IsAny<Func<int, Stream>>()), Times.Never);
                _pdfModule.Verify(
                    m =>
                        m.SetObjectSetting(
                            It.Is<IntPtr>(v => v == objectSettingsPtr),
                            It.Is<string>(v => v == "toc.captionText"),
                            It.Is<string?>(v => v == captionText)),
                    Times.Once);
                _pdfModule.Verify(m => m.DestroyObjectSetting(It.IsAny<IntPtr>()), Times.Once);
                _module.Verify(m => m.DestroyGlobalSetting(It.IsAny<IntPtr>()), Times.Once);
                _module.Verify(m => m.DestroyConverter(It.IsAny<IntPtr>()), Times.Once);
                _module.Verify(m => m.Terminate(), Times.Once);
                result.Should().BeFalse();
            }
        }
#pragma warning restore MA0051 // Method is too long

#pragma warning disable MA0051 // Method is too long
        [Theory]
        [MemberData(nameof(GetTestData))]
        public void ConvertImplShouldReturnStreamWhenConverted(
            string? htmlContent,
            byte[]? htmlContentByteArray,
            Stream? htmlContentStream)
        {
            // Arrange
            using var memoryStream = new MemoryStream();
            var globalSettingsPtr = new IntPtr(_fixture.Create<int>());
            var objectSettingsPtr = new IntPtr(_fixture.Create<int>());
            var converterPtr = new IntPtr(_fixture.Create<int>());
            _module.Setup(m => m.Initialize(It.IsAny<int>()))
                .Returns(1);
            _module.Setup(m =>
                    m.CreateGlobalSettings())
                .Returns(globalSettingsPtr);
            _module.Setup(m =>
                    m.CreateConverter(It.IsAny<IntPtr>()))
                .Returns(converterPtr);
            _module.Setup(
                m =>
                    m.SetGlobalSetting(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<string?>()));
            _module.Setup(m => m.Convert(It.IsAny<IntPtr>()))
                .Returns(true);
            _module.Setup(m => m.GetOutput(It.IsAny<IntPtr>(), It.IsAny<Func<int, Stream>>()));
            _module.Setup(m => m.DestroyGlobalSetting(It.IsAny<IntPtr>()));
            _module.Setup(m => m.DestroyConverter(It.IsAny<IntPtr>()));
            _module.Setup(m => m.Terminate());
            _pdfModule.Setup(m =>
                    m.CreateObjectSettings())
                .Returns(objectSettingsPtr);
            _pdfModule.Setup(
                m =>
                    m.SetObjectSetting(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<string?>()));
            _pdfModule.Setup(m => m.DestroyObjectSetting(It.IsAny<IntPtr>()));
            var document = new HtmlToPdfDocument();
            var documentTitle = _fixture.Create<string>();
            var captionText = _fixture.Create<string>();
            document.GlobalSettings.DocumentTitle = documentTitle;
            document.ObjectSettings.Add(
                new PdfObjectSettings
                {
                    CaptionText = captionText,
                    HtmlContent = htmlContent,
                    HtmlContentByteArray = htmlContentByteArray,
                    HtmlContentStream = htmlContentStream,
                });

            // Act
            // ReSharper disable once AccessToDisposedClosure
            var result = _sut.ConvertImpl(document, _ => memoryStream);

            // Assert
            using (new AssertionScope())
            {
                _module.Verify(m => m.Initialize(It.IsAny<int>()), Times.Once);
                _module.Verify(m => m.CreateGlobalSettings(), Times.Once);
                _module.Verify(
                    m =>
                        m.SetGlobalSetting(
                            It.Is<IntPtr>(v => v == globalSettingsPtr),
                            It.Is<string>(v => v == "documentTitle"),
                            It.Is<string?>(v => v == documentTitle)),
                    Times.Once);
                _pdfModule.Verify(m => m.CreateObjectSettings(), Times.Once);
                _module.Verify(m => m.GetOutput(It.IsAny<IntPtr>(), It.IsAny<Func<int, Stream>>()), Times.Once);
                _pdfModule.Verify(
                    m =>
                        m.SetObjectSetting(
                            It.Is<IntPtr>(v => v == objectSettingsPtr),
                            It.Is<string>(v => v == "toc.captionText"),
                            It.Is<string?>(v => v == captionText)),
                    Times.Once);
                _pdfModule.Verify(m => m.DestroyObjectSetting(It.IsAny<IntPtr>()), Times.Once);
                _module.Verify(m => m.DestroyGlobalSetting(It.IsAny<IntPtr>()), Times.Once);
                _module.Verify(m => m.DestroyConverter(It.IsAny<IntPtr>()), Times.Once);
                _module.Verify(m => m.Terminate(), Times.Once);
                result.Should().BeTrue();
            }
        }
#pragma warning restore MA0051 // Method is too long

        [Fact]
        public void AddContentShouldThrowExceptionWhenNullPdfObjectSettingsPassed()
        {
            // Arrange
            var converterPtr = new IntPtr(_fixture.Create<int>());
            var objectSettingsPtr = new IntPtr(_fixture.Create<int>());
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Action action = () => _sut.AddContent(converterPtr, objectSettingsPtr, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            // Act & Assert
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void AddContentShouldThrowExceptionWhenAllHtmlContentNullPassed()
        {
            // Arrange
            var converterPtr = new IntPtr(_fixture.Create<int>());
            var objectSettingsPtr = new IntPtr(_fixture.Create<int>());
            var pdfObjectSettings = _fixture.Build<PdfObjectSettings>()
                .Without(s => s.HtmlContent)
                .Without(s => s.HtmlContentByteArray)
                .Without(s => s.HtmlContentStream)
                .Create();

            Action action = () => _sut.AddContent(converterPtr, objectSettingsPtr, pdfObjectSettings);

            // Act & Assert
            action.Should().Throw<HtmlContentEmptyException>();
        }

        [Fact]
        public void AddContentStringShouldThrowExceptionWhenNullPassed()
        {
            // Arrange
            var converterPtr = new IntPtr(_fixture.Create<int>());
            var objectSettingsPtr = new IntPtr(_fixture.Create<int>());

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Action action = () => _sut.AddContentString(converterPtr, objectSettingsPtr, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            // Act & Assert
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void AddContentStringShouldThrowExceptionWhenHtmlContentNullPassed()
        {
            // Arrange
            var converterPtr = new IntPtr(_fixture.Create<int>());
            var objectSettingsPtr = new IntPtr(_fixture.Create<int>());
            var pdfObjectSettings = _fixture.Build<PdfObjectSettings>()
                .Without(s => s.HtmlContent)
                .Without(s => s.HtmlContentByteArray)
                .Without(s => s.HtmlContentStream)
                .Create();

            Action action = () => _sut.AddContentString(converterPtr, objectSettingsPtr, pdfObjectSettings);

            // Act & Assert
            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void AddContentStreamShouldThrowExceptionWhenNullPassed()
        {
            // Arrange
            var converterPtr = new IntPtr(_fixture.Create<int>());
            var objectSettingsPtr = new IntPtr(_fixture.Create<int>());

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Action action = () => _sut.AddContentStream(converterPtr, objectSettingsPtr, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            // Act & Assert
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void AddContentStreamShouldThrowExceptionWhenTooLargeStreamPassed()
        {
            // Arrange
            var converterPtr = new IntPtr(_fixture.Create<int>());
            var objectSettingsPtr = new IntPtr(_fixture.Create<int>());
            var streamMock = new Mock<Stream>();
            streamMock.SetupGet(s => s.Length)
                .Returns(int.MaxValue + 1L);

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Action action = () => _sut.AddContentStream(converterPtr, objectSettingsPtr, streamMock.Object);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

            // Act & Assert
            action.Should().Throw<HtmlContentStreamTooLargeException>();
        }
    }
}
