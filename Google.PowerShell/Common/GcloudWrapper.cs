﻿// Copyright 2016 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Google.Apis.Auth.OAuth2.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Threading.Tasks;

namespace Google.PowerShell.Common
{
    /// <summary>
    /// This class wraps the gcloud command and offers up some of its services in a
    /// as async methods. 
    /// </summary>
    public static class GCloudWrapper
    {
        /// <summary>
        /// Returns the 
        /// </summary>
        /// <returns></returns>
        public static string GetInstallationPropertiesPath()
        {
            string gCloudInfoOutput = GetGCloudCommandOutput("info");
            JToken gCloudInfoJson = JObject.Parse(gCloudInfoOutput);

            try
            {
                // SelectToken will throw NullReferenceException if the token cannot be found.
                gCloudInfoJson = gCloudInfoJson.SelectToken("config.paths.installation_properties_path");
                
                if (gCloudInfoJson.Type == JTokenType.String)
                {
                    return gCloudInfoJson.Value<string>();
                }
            }
            catch (NullReferenceException)
            {
                // Throw exception at the end.
            }

            throw new FileNotFoundException("Installation Properties file for Google Cloud SDK cannot be found.");
        }

        public static TokenResponse GetAccessToken()
        {
            string accessToken = GetGCloudCommandOutput("auth print-access-token");
            DateTime issuedTime = DateTime.Now;
            JToken tokenJson = JObject.Parse(accessToken);

            try
            {
                // SelectToken will throw NullReferenceException if the token cannot be found.
                tokenJson = tokenJson.SelectToken("token_response");

                TokenResponse token = tokenJson.ToObject<TokenResponse>();
                token.Issued = issuedTime;

                return token;
            }
            catch (NullReferenceException)
            {
                // Throw exception at the end.
            }
            catch (JsonException)
            {
                // Throw exception at the end.
            }

            throw new InvalidDataException("Failed to get access token from gcloud auth print-access-token.");
        }

        private static string GetGCloudCommandOutput(string command, IDictionary<string, string> environment = null)
        {
            var actualCommand = $"gcloud {command} --format=json";
            // This code depends on the fact that gcloud.cmd is a batch file.
            Debug.Write($"Executing gcloud command: {actualCommand}");
            ProcessOutput processOutput = ProcessUtils.GetCommandOutput("cmd.exe", $"/c {actualCommand}", environment);
            if (processOutput.Succeeded)
            {
                return processOutput.StandardOutput;
            }

            if (!string.IsNullOrWhiteSpace(processOutput.StandardError))
            {
                throw new InvalidOperationException($"Command {actualCommand} failed with error: processOutput.StandardError");
            }

            throw new InvalidOperationException($"Command {actualCommand} failed.");
        }
    }
}