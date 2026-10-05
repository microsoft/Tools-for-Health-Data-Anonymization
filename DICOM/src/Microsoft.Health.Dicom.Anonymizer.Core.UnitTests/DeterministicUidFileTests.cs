// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FellowOakDicom;
using Microsoft.Health.Dicom.Anonymizer.Core.Models;
using Xunit;

namespace Microsoft.Health.Dicom.Anonymizer.Core.UnitTests
{
    public class DeterministicUidFileTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task GivenOriginalFiles_WhenTwoRealWorkersAndRetriesRun_OnlyOptInSurvivesProcessBoundaries(bool deterministic)
        {
            string directory = Path.Combine(Path.GetTempPath(), "dicom-uid-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var first = DeterministicUidTests.File();
                first.Dataset.Add(new DicomSequence(DicomTag.ReferencedImageSequence, new DicomDataset
                {
                    { DicomTag.ReferencedSOPClassUID, DicomUID.MRImageStorage },
                    { DicomTag.ReferencedSOPInstanceUID, "2.25.124" },
                }));
                var second = DeterministicUidTests.File("2.25.124");
                second.Dataset.AddOrUpdate(DicomTag.SOPClassUID, DicomUID.MRImageStorage);
                second.FileMetaInfo.MediaStorageSOPClassUID = DicomUID.MRImageStorage;
                second.Dataset.AddOrUpdate(DicomTag.SeriesInstanceUID, "2.25.102");
                second.Dataset.Add(new DicomSequence(DicomTag.ReferencedImageSequence, new DicomDataset
                {
                    { DicomTag.ReferencedSOPClassUID, DicomUID.CTImageStorage },
                    { DicomTag.ReferencedSOPInstanceUID, "2.25.123" },
                }));
                string[] sources = { Path.Combine(directory, "a.dcm"), Path.Combine(directory, "b.dcm") };
                first.Save(sources[0]);
                second.Save(sources[1]);
                var originals = sources.Select(File.ReadAllBytes).ToArray();

                await Task.WhenAll(RunWorker(directory, "one", deterministic), RunWorker(directory, "two", deterministic));

                Assert.NotEqual(File.ReadAllText(Path.Combine(directory, "one.pid")), File.ReadAllText(Path.Combine(directory, "two.pid")));
                foreach (string worker in new[] { "one", "two" })
                {
                    var a = OpenOutput(directory, worker, "a");
                    var b = OpenOutput(directory, worker, "b");
                    Assert.Equal(a.Dataset.GetString(DicomTag.StudyInstanceUID), b.Dataset.GetString(DicomTag.StudyInstanceUID));
                    Assert.NotEqual(a.Dataset.GetString(DicomTag.SeriesInstanceUID), b.Dataset.GetString(DicomTag.SeriesInstanceUID));
                    Assert.Equal(a.Dataset.GetString(DicomTag.SOPInstanceUID), Reference(b));
                    Assert.Equal(b.Dataset.GetString(DicomTag.SOPInstanceUID), Reference(a));
                    foreach (string name in new[] { "a", "b" })
                    {
                        var output = OpenOutput(directory, worker, name);
                        var retry = OpenOutput(directory, worker, name + "-retry");
                        Assert.Equal(output.Dataset.GetString(DicomTag.SOPInstanceUID), retry.Dataset.GetString(DicomTag.SOPInstanceUID));
                        Assert.Equal(Reference(output), Reference(retry));
                        Assert.Equal(output.Dataset.GetString(DicomTag.SOPInstanceUID), output.FileMetaInfo.MediaStorageSOPInstanceUID.UID);
                        Assert.Equal(output.Dataset.GetSingleValue<DicomUID>(DicomTag.SOPClassUID), output.FileMetaInfo.MediaStorageSOPClassUID);
                        Assert.Equal(DicomTransferSyntax.ExplicitVRLittleEndian, output.FileMetaInfo.TransferSyntax);
                        Assert.Equal(new byte[] { 1, 2, 3, 4 }, output.Dataset.GetValues<byte>(DicomTag.PixelData));
                        Assert.False(output.Dataset.Contains(DicomTag.PatientName));
                        output.Dataset.Validate();
                    }

                    Assert.Equal(DicomUID.MRImageStorage.UID, a.Dataset.GetSequence(DicomTag.ReferencedImageSequence).Items[0].GetString(DicomTag.ReferencedSOPClassUID));
                    Assert.Equal(DicomUID.CTImageStorage.UID, b.Dataset.GetSequence(DicomTag.ReferencedImageSequence).Items[0].GetString(DicomTag.ReferencedSOPClassUID));
                }

                var one = OpenOutput(directory, "one", "a");
                var two = OpenOutput(directory, "two", "a-retry");
                foreach (var tag in new[] { DicomTag.StudyInstanceUID, DicomTag.SeriesInstanceUID, DicomTag.SOPInstanceUID })
                {
                    Assert.Equal(deterministic, one.Dataset.GetString(tag) == two.Dataset.GetString(tag));
                }

                if (deterministic)
                {
                    Assert.Equal(DeterministicUidTests.Expected, one.Dataset.GetString(DicomTag.SOPInstanceUID));
                }

                for (int index = 0; index < sources.Length; index++)
                {
                    Assert.Equal(originals[index], File.ReadAllBytes(sources[index]));
                }
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public void GivenWorkerRequest_WhenInvokedInChildTestHost_WritesAndReopensActualDicomFiles()
        {
            string? directory = Environment.GetEnvironmentVariable("DICOM_UID_TEST_DIRECTORY");
            if (directory == null)
            {
                return;
            }

            string worker = Environment.GetEnvironmentVariable("DICOM_UID_TEST_WORKER") ?? throw new InvalidOperationException("Worker identity is required.");
            string mode = Environment.GetEnvironmentVariable("DICOM_UID_TEST_MODE") ?? throw new InvalidOperationException("Worker mode is required.");
            Assert.Contains(mode, new[] { "legacy", UidMappingSettings.HmacSha256128V1 });
            var keys = mode == "legacy" ? new RuntimeKeySettings() : DeterministicUidTests.Keys();
            var engine = DeterministicUidTests.Engine();
            string[] names = worker == "one" ? new[] { "a", "b" } : new[] { "b", "a" };
            foreach (string name in names)
            {
                for (int retry = 0; retry < 2; retry++)
                {
                    var source = DicomFile.Open(Path.Combine(directory, name + ".dcm"), FileReadOption.ReadAll);
                    var output = engine.AnonymizeFile(source, keys);
                    string destination = Path.Combine(directory, worker + "-" + name + (retry == 0 ? string.Empty : "-retry") + ".dcm");
                    output.Save(destination);
                    var reopened = DicomFile.Open(destination, FileReadOption.ReadAll);
                    Assert.Equal(output.Dataset.GetString(DicomTag.SOPInstanceUID), reopened.FileMetaInfo.MediaStorageSOPInstanceUID.UID);
                }
            }

            File.WriteAllText(Path.Combine(directory, worker + ".pid"), Environment.ProcessId.ToString());
        }

        [Theory]
        [InlineData("I290.dcm")]
        [InlineData("I341.dcm")]
        [InlineData("lung.dcm")]
        public void GivenPublicSample_WhenFileApiOptedIn_SourcePixelsSyntaxAndClassArePreserved(string name)
        {
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "samples", name)))
            {
                root = root.Parent;
            }

            if (root == null)
            {
                throw new InvalidOperationException("Public DICOM samples directory was not found.");
            }

            string path = Path.Combine(root.FullName, "samples", name);
            byte[] originalBytes = File.ReadAllBytes(path);
            var source = DicomFile.Open(path, FileReadOption.ReadAll);
            var engine = DeterministicUidTests.Engine();
            var output = engine.AnonymizeFile(source, DeterministicUidTests.Keys());
            using var stream = new MemoryStream();
            output.Save(stream);
            stream.Position = 0;
            var reopened = DicomFile.Open(stream, FileReadOption.ReadAll);
            Assert.NotEqual(source.Dataset.GetString(DicomTag.SOPInstanceUID), reopened.Dataset.GetString(DicomTag.SOPInstanceUID));
            Assert.Equal(reopened.Dataset.GetString(DicomTag.SOPInstanceUID), reopened.FileMetaInfo.MediaStorageSOPInstanceUID.UID);
            Assert.Equal(source.FileMetaInfo.TransferSyntax, reopened.FileMetaInfo.TransferSyntax);
            Assert.Equal(source.FileMetaInfo.MediaStorageSOPClassUID, reopened.FileMetaInfo.MediaStorageSOPClassUID);
            var originalPixels = source.Dataset.GetDicomItem<DicomItem>(DicomTag.PixelData);
            var outputPixels = reopened.Dataset.GetDicomItem<DicomItem>(DicomTag.PixelData);
            Assert.Equal(originalPixels.ValueRepresentation, outputPixels.ValueRepresentation);
            if (originalPixels is DicomFragmentSequence fragments)
            {
                var outputFragments = Assert.IsAssignableFrom<DicomFragmentSequence>(outputPixels);
                Assert.Equal(fragments.OffsetTable, outputFragments.OffsetTable);
                var before = fragments.ToArray();
                var after = outputFragments.ToArray();
                Assert.Equal(before.Length, after.Length);
                for (int index = 0; index < before.Length; index++)
                {
                    Assert.Equal(before[index].Data, after[index].Data);
                }
            }
            else
            {
                Assert.Equal(Assert.IsAssignableFrom<DicomElement>(originalPixels).Buffer.Data, Assert.IsAssignableFrom<DicomElement>(outputPixels).Buffer.Data);
            }

            Assert.Equal(originalBytes, File.ReadAllBytes(path));
        }

        private static DicomFile OpenOutput(string directory, string worker, string name) =>
            DicomFile.Open(Path.Combine(directory, worker + "-" + name + ".dcm"), FileReadOption.ReadAll);

        private static string Reference(DicomFile file) =>
            file.Dataset.GetSequence(DicomTag.ReferencedImageSequence).Items[0].GetString(DicomTag.ReferencedSOPInstanceUID);

        private static async Task RunWorker(string directory, string worker, bool deterministic)
        {
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            start.ArgumentList.Add("vstest");
            start.ArgumentList.Add(typeof(DeterministicUidFileTests).Assembly.Location);
            start.ArgumentList.Add("--TestCaseFilter:FullyQualifiedName=Microsoft.Health.Dicom.Anonymizer.Core.UnitTests.DeterministicUidFileTests.GivenWorkerRequest_WhenInvokedInChildTestHost_WritesAndReopensActualDicomFiles");
            start.Environment["DICOM_UID_TEST_DIRECTORY"] = directory;
            start.Environment["DICOM_UID_TEST_WORKER"] = worker;
            start.Environment["DICOM_UID_TEST_MODE"] = deterministic ? UidMappingSettings.HmacSha256128V1 : "legacy";
            using var process = Process.Start(start) ?? throw new InvalidOperationException("UID worker could not start.");
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            Assert.True(process.ExitCode == 0, await stdout + await stderr);
        }
    }
}
