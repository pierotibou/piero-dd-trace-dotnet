// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2022 Datadog, Inc.

#include "gmock/gmock.h"
#include "gtest/gtest.h"

#include "LibddprofExporter.h"
#include "OpSysTools.h"

#include "ProfilerMockedInterface.h"

#include "shared/src/native-src/dd_filesystem.hpp"

using ::testing::_;
using ::testing::Return;
using ::testing::ReturnRef;
using ::testing::Throw;

std::string ComputeExpectedFilePrefix(const std::string& applicationName)
{
    std::ostringstream expectedFilePrefix;
    expectedFilePrefix << applicationName << "_" << OpSysTools::GetProcId() << "_";
    return expectedFilePrefix.str();
}

TEST(LibddprofExporterTest, CheckProfileIsWrittenToDisk)
{
    auto [configuration, mockConfiguration] = CreateConfiguration();

    fs::path pprofTempDir = fs::temp_directory_path() / tmpnam(nullptr);
    EXPECT_CALL(mockConfiguration, GetProfilesOutputDirectory()).Times(1).WillOnce(ReturnRef(pprofTempDir));

    std::string agentUrl;
    EXPECT_CALL(mockConfiguration, GetAgentUrl()).Times(1).WillOnce(ReturnRef(agentUrl));

    std::string agentHost = "localhost";
    EXPECT_CALL(mockConfiguration, GetAgentHost()).Times(1).WillOnce(ReturnRef(agentHost));
    int agentPort = 8126;
    EXPECT_CALL(mockConfiguration, GetAgentPort()).Times(1).WillOnce(Return(agentPort));
    std::string version = "1.0.2";
    EXPECT_CALL(mockConfiguration, GetVersion()).Times(1).WillOnce(ReturnRef(version));
    std::string env = "myenv";
    EXPECT_CALL(mockConfiguration, GetEnvironment()).Times(1).WillOnce(ReturnRef(env));
    std::string host = "localhost";
    EXPECT_CALL(mockConfiguration, GetHostname()).Times(1).WillOnce(ReturnRef(host));
    EXPECT_CALL(mockConfiguration, IsAgentless()).Times(1).WillOnce(Return(false));

    std::vector<std::pair<std::string, std::string>> tags;
    EXPECT_CALL(mockConfiguration, GetUserTags()).Times(1).WillOnce(ReturnRef(tags));

    auto applicationStore = MockApplicationStore();

    std::string firstRid = "MyRid";
    std::string firstApplication = "MyApp";

    std::string secondRid = "MyRid2";
    std::string secondApplication = "OtherApplication";

    EXPECT_CALL(applicationStore, GetName(std::string_view(firstRid))).WillRepeatedly(ReturnRef(firstApplication));
    EXPECT_CALL(applicationStore, GetName(std::string_view(secondRid))).WillRepeatedly(ReturnRef(secondApplication));

    auto exporter = LibddprofExporter(&mockConfiguration, &applicationStore);


    // create samples for the same application/runtime id
    auto callstack1 = std::initializer_list<std::pair<std::string, std::string>>({{"module", "frame1"}, {"module", "frame2"}, {"module", "frame3"}});
    auto labels1 = std::initializer_list<std::pair<std::string, std::string>>{{"label1", "value1"}, {"label2", "value2"}};
    auto sample1 = CreateSample(firstRid,
                                callstack1,
                                labels1,
                                21);

    auto callstack2 = std::initializer_list<std::pair<std::string, std::string>>({{"module", "frame1"}, {"module", "frame2"}, {"module", "frame3"}, {"module", "frame4"}});
    auto labels2 = std::initializer_list<std::pair<std::string, std::string>>{{"label1", "value1"}, {"label2", "value2"}, {"label3", "value3"}};
    auto sample2 = CreateSample(firstRid,
                                callstack2,
                                labels2,
                                42);

    auto callstack3 = std::initializer_list<std::pair<std::string, std::string>>({{"module", "frame1"}, {"module", "frame2"}});
    auto labels3 = std::initializer_list<std::pair<std::string, std::string>>{{"label1", "value1"}};
    auto sample3 = CreateSample(firstRid,
                                callstack3,
                                labels3,
                                84);
    exporter.Add(sample1);
    exporter.Add(sample2);
    exporter.Add(sample3);

    exporter.Export();

    std::string expectedPrefix = ComputeExpectedFilePrefix(firstApplication);

    std::vector<fs::directory_entry> pprofFiles;
    for (auto const& file : fs::directory_iterator(pprofTempDir))
    {
        pprofFiles.push_back(file);
    }

    ASSERT_EQ(pprofFiles.size(), 1);

    auto file = pprofFiles[0];

    ASSERT_TRUE(file.is_regular_file());

    std::string filename = file.path().filename().string();

    ASSERT_THAT(filename, ::testing::StartsWith(expectedPrefix));

    fs::remove_all(pprofTempDir);
}


TEST(LibddprofExporterTest, EnsureTwoPprofFilesAreWrittenToDiskForTwoApplications)
{
    auto [configuration, mockConfiguration] = CreateConfiguration();

    fs::path pprofTempDir = fs::temp_directory_path() / tmpnam(nullptr);
    EXPECT_CALL(mockConfiguration, GetProfilesOutputDirectory()).Times(1).WillOnce(ReturnRef(pprofTempDir));

    std::string agentUrl;
    EXPECT_CALL(mockConfiguration, GetAgentUrl()).Times(1).WillOnce(ReturnRef(agentUrl));

    std::string agentHost = "localhost";
    EXPECT_CALL(mockConfiguration, GetAgentHost()).Times(1).WillOnce(ReturnRef(agentHost));
    int agentPort = 8126;
    EXPECT_CALL(mockConfiguration, GetAgentPort()).Times(1).WillOnce(Return(agentPort));
    std::string version = "1.0.2";
    EXPECT_CALL(mockConfiguration, GetVersion()).Times(1).WillOnce(ReturnRef(version));
    std::string env = "myenv";
    EXPECT_CALL(mockConfiguration, GetEnvironment()).Times(1).WillOnce(ReturnRef(env));
    std::string host = "localhost";
    EXPECT_CALL(mockConfiguration, GetHostname()).Times(1).WillOnce(ReturnRef(host));
    EXPECT_CALL(mockConfiguration, IsAgentless()).Times(1).WillOnce(Return(false));

    std::vector<std::pair<std::string, std::string>> tags;
    EXPECT_CALL(mockConfiguration, GetUserTags()).Times(1).WillOnce(ReturnRef(tags));

    auto applicationStore = MockApplicationStore();

    std::string firstRid = "MyRid";
    std::string firstApplication = "MyApp";

    std::string secondRid = "MyRid2";
    std::string secondApplication = "OtherApplication";

    EXPECT_CALL(applicationStore, GetName(std::string_view(firstRid))).WillRepeatedly(ReturnRef(firstApplication));
    EXPECT_CALL(applicationStore, GetName(std::string_view(secondRid))).WillRepeatedly(ReturnRef(secondApplication));

    auto exporter = LibddprofExporter(&mockConfiguration, &applicationStore);

    auto callstack1 = std::initializer_list<std::pair<std::string, std::string>>({{"module", "frame1"}, {"module", "frame2"}, {"module", "frame3"}});
    auto labels1 = std::initializer_list<std::pair<std::string, std::string>>{{"label1", "value1"}, {"label2", "value2"}};
    auto sample1 = CreateSample(firstRid,
                                callstack1,
                                labels1,
                                21);

    auto callstack2 = std::initializer_list<std::pair<std::string, std::string>>({{"module", "frame1"}, {"module", "frame2"}, {"module", "frame3"}, {"module", "frame4"}});
    auto labels2 = std::initializer_list<std::pair<std::string, std::string>>{{"label1", "value1"}, {"label2", "value2"}, {"label3", "value3"}};
    auto sample2 = CreateSample(secondRid,
                                callstack2,
                                labels2,
                                42);

    exporter.Add(sample1);
    exporter.Add(sample2);

    exporter.Export();

    std::string expectFirstFilePrefix = ComputeExpectedFilePrefix(firstApplication);
    std::string expectedSecondFilePrefix = ComputeExpectedFilePrefix(secondApplication);

    std::vector<fs::directory_entry> pprofFiles;
    for (auto const& file : fs::directory_iterator(pprofTempDir))
    {
        pprofFiles.push_back(file);
    }

    ASSERT_EQ(pprofFiles.size(), 2);

    auto firstFile = pprofFiles[0];
    auto secondFile = pprofFiles[1];

    ASSERT_TRUE(firstFile.is_regular_file());
    ASSERT_TRUE(secondFile.is_regular_file());

    std::string firstFilename = firstFile.path().filename().string();
    std::string secondFilename = secondFile.path().filename().string();

    // check if firstFilename starts with expectFirstFilePrefix
    if (firstFilename.rfind(expectFirstFilePrefix) != 0)
    {
        // no, we make sure that firstFilename starts with expectedSecondFilePrefix
        // and secondFilename starts with expectFirstFilePrefix
        ASSERT_THAT(firstFilename, ::testing::StartsWith(expectedSecondFilePrefix));
        ASSERT_THAT(secondFilename, ::testing::StartsWith(expectFirstFilePrefix));
    }
    else
    {
        // firstFilename starts with expectFirstFilePrefix
        ASSERT_THAT(secondFilename, ::testing::StartsWith(expectedSecondFilePrefix));
    }

    fs::remove_all(pprofTempDir);
}

TEST(LibddprofExporterTest, MustCreateAgentBasedExporterIfAgentUrlIsSet)
{
    auto [configuration, mockConfiguration] = CreateMockForUniquePtr<IConfiguration, MockConfiguration>();

    std::string agentUrl = "http://host::port";
    EXPECT_CALL(mockConfiguration, GetAgentUrl()).Times(1).WillOnce(ReturnRef(agentUrl));
    EXPECT_CALL(mockConfiguration, GetAgentHost()).Times(0);
    EXPECT_CALL(mockConfiguration, GetAgentPort()).Times(0);

    std::string version = "1.0.2";
    EXPECT_CALL(mockConfiguration, GetVersion()).Times(1).WillOnce(ReturnRef(version));
    std::string env = "myenv";
    EXPECT_CALL(mockConfiguration, GetEnvironment()).Times(1).WillOnce(ReturnRef(env));
    std::string host = "localhost";
    EXPECT_CALL(mockConfiguration, GetHostname()).Times(1).WillOnce(ReturnRef(host));

    // only used in agentless case
    EXPECT_CALL(mockConfiguration, IsAgentless()).Times(1).WillOnce(Return(false));
    EXPECT_CALL(mockConfiguration, GetSite()).Times(0);
    EXPECT_CALL(mockConfiguration, GetApiKey()).Times(0);

    fs::path pprofDir;
    EXPECT_CALL(mockConfiguration, GetProfilesOutputDirectory()).Times(1).WillOnce(ReturnRef(pprofDir));

    std::vector<std::pair<std::string, std::string>> tags;
    EXPECT_CALL(mockConfiguration, GetUserTags()).Times(1).WillOnce(ReturnRef(tags));

    auto applicationStore = MockApplicationStore();

    auto exporter = LibddprofExporter(&mockConfiguration, &applicationStore);
}

TEST(LibddprofExporterTest, MustCreateAgentBasedExporterIfAgentUrlIsNotSet)
{
    auto [configuration, mockConfiguration] = CreateMockForUniquePtr<IConfiguration, MockConfiguration>();

    std::string agentUrl = "";
    EXPECT_CALL(mockConfiguration, GetAgentUrl()).Times(1).WillOnce(ReturnRef(agentUrl));
    std::string agentHost = "localhost";
    EXPECT_CALL(mockConfiguration, GetAgentHost()).Times(1).WillOnce(ReturnRef(agentHost));
    int agentPort = 8126;
    EXPECT_CALL(mockConfiguration, GetAgentPort()).Times(1).WillOnce(Return(agentPort));

    std::string version = "1.0.2";
    EXPECT_CALL(mockConfiguration, GetVersion()).Times(1).WillOnce(ReturnRef(version));
    std::string env = "myenv";
    EXPECT_CALL(mockConfiguration, GetEnvironment()).Times(1).WillOnce(ReturnRef(env));
    std::string host = "localhost";
    EXPECT_CALL(mockConfiguration, GetHostname()).Times(1).WillOnce(ReturnRef(host));

    // only used in agentless case
    EXPECT_CALL(mockConfiguration, IsAgentless()).Times(1).WillOnce(Return(false));
    EXPECT_CALL(mockConfiguration, GetSite()).Times(0);
    EXPECT_CALL(mockConfiguration, GetApiKey()).Times(0);

    fs::path pprofDir;
    EXPECT_CALL(mockConfiguration, GetProfilesOutputDirectory()).Times(1).WillOnce(ReturnRef(pprofDir));

    std::vector<std::pair<std::string, std::string>> tags;
    EXPECT_CALL(mockConfiguration, GetUserTags()).Times(1).WillOnce(ReturnRef(tags));

    auto applicationStore = MockApplicationStore();

    auto exporter = LibddprofExporter(&mockConfiguration, &applicationStore);
}

TEST(LibddprofExporterTest, MustCreateAgentLessExporterIfAgentless)
{
    auto [configuration, mockConfiguration] = CreateConfiguration();

    EXPECT_CALL(mockConfiguration, IsAgentless()).Times(1).WillOnce(Return(true));
    std::string site = "test_site";
    EXPECT_CALL(mockConfiguration, GetSite()).Times(1).WillOnce(ReturnRef(site));
    std::string apiKey = "4224";
    EXPECT_CALL(mockConfiguration, GetApiKey()).Times(1).WillOnce(ReturnRef(apiKey));

    // not called when agentless
    EXPECT_CALL(mockConfiguration, GetAgentUrl()).Times(0);
    EXPECT_CALL(mockConfiguration, GetAgentHost()).Times(0);
    EXPECT_CALL(mockConfiguration, GetAgentPort()).Times(0);

    std::string version = "1.0.2";
    EXPECT_CALL(mockConfiguration, GetVersion()).Times(1).WillOnce(ReturnRef(version));
    std::string env = "myenv";
    EXPECT_CALL(mockConfiguration, GetEnvironment()).Times(1).WillOnce(ReturnRef(env));
    std::string host = "localhost";
    EXPECT_CALL(mockConfiguration, GetHostname()).Times(1).WillOnce(ReturnRef(host));

    fs::path pprofDir;
    EXPECT_CALL(mockConfiguration, GetProfilesOutputDirectory()).Times(1).WillOnce(ReturnRef(pprofDir));

    std::vector<std::pair<std::string, std::string>> tags;
    EXPECT_CALL(mockConfiguration, GetUserTags()).Times(1).WillOnce(ReturnRef(tags));

    auto applicationStore = MockApplicationStore();

    auto exporter = LibddprofExporter(&mockConfiguration, &applicationStore);
}

TEST(LibddprofExporterTest, MakeSureNoCrashForReallyLongCallstack)
{
    auto [configuration, mockConfiguration] = CreateConfiguration();

    fs::path pprofTempDir;
    EXPECT_CALL(mockConfiguration, GetProfilesOutputDirectory()).Times(1).WillOnce(ReturnRef(pprofTempDir));

    std::string agentUrl;
    EXPECT_CALL(mockConfiguration, GetAgentUrl()).Times(1).WillOnce(ReturnRef(agentUrl));

    std::string agentHost = "localhost";
    EXPECT_CALL(mockConfiguration, GetAgentHost()).Times(1).WillOnce(ReturnRef(agentHost));
    int agentPort = 8126;
    EXPECT_CALL(mockConfiguration, GetAgentPort()).Times(1).WillOnce(Return(agentPort));
    std::string version = "1.0.2";
    EXPECT_CALL(mockConfiguration, GetVersion()).Times(1).WillOnce(ReturnRef(version));
    std::string env = "myenv";
    EXPECT_CALL(mockConfiguration, GetEnvironment()).Times(1).WillOnce(ReturnRef(env));
    std::string host = "localhost";
    EXPECT_CALL(mockConfiguration, GetHostname()).Times(1).WillOnce(ReturnRef(host));
    EXPECT_CALL(mockConfiguration, IsAgentless()).Times(1).WillOnce(Return(false));

    std::vector<std::pair<std::string, std::string>> tags;
    EXPECT_CALL(mockConfiguration, GetUserTags()).Times(1).WillOnce(ReturnRef(tags));

    auto applicationStore = MockApplicationStore();

    auto exporter = LibddprofExporter(&mockConfiguration, &applicationStore);

    std::string runtimeId = "MyRid";
    auto callstack = CreateCallstack(2048);
    auto labels = std::initializer_list<std::pair<std::string, std::string>>{{"label1", "value1"}, {"label2", "value2"}};
    auto sample1 = CreateSample(runtimeId, callstack,
                                labels,
                                42);

    EXPECT_NO_THROW(exporter.Add(sample1));
}