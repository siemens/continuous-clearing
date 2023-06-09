// --------------------------------------------------------------------------------------------------------------------
// SPDX-FileCopyrightText: 2023 Siemens AG
//
//  SPDX-License-Identifier: MIT
// -------------------------------------------------------------------------------------------------------------------- 

using CycloneDX.Json;
using CycloneDX.Models;
using log4net;
using log4net.Core;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;

namespace LCT.Common
{
    public class CycloneDXBomParser : ICycloneDXBomParser
    {
        static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public Bom ParseCycloneDXBom(string filePath)
        {
            Bom bom = new Bom();
            string json = string.Empty;
            Logger.Logger.Log(null, Level.Notice, $"\nConsuming CycloneBOM Data...", null);

            try
            {
                json = File.ReadAllText(filePath);
                bom = JsonConvert.DeserializeObject<Bom>(json);
            }
            catch (JsonSerializationException)
            {
                bom = Serializer.Deserialize(json);
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error("Exception in reading cycloneDx bom", ex);
            }
            catch (FileNotFoundException ex)
            {
                Logger.Error("Exception in reading cycloneDx bom", ex);
            }
            catch (JsonReaderException ex)
            {
                Logger.Error("Exception in reading cycloneDx bom", ex);
            }
            return bom;
        }
    }
}
