using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Win32;

namespace SEBlueprintCalc
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            UpdateData();
        }

        public string rootDir = "../"; //Directory.GetCurrentDirectory();

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.FileName = ".bp file";
            openFileDialog1.ShowDialog();

            try
            {
                var bpFile = File.ReadAllText(openFileDialog1.FileName);
                pictureBox1.Image = Image.FromFile(Path.GetDirectoryName(openFileDialog1.FileName) + "\\thumb.png");
                label1.Text = Path.GetFileName(Path.GetDirectoryName(openFileDialog1.FileName));
                Dictionary<string, int> bpBlocks = readXMLBlueprintBlocks(bpFile);
                Dictionary<string, int> bpComps = getComponents(bpBlocks);
                Dictionary<string, float> bpIngots = getIngots(bpComps);

                dataGridView1.DataSource = (from entry in bpComps orderby entry.Value descending select entry).ToList();
                dataGridView2.DataSource = (from entry in bpBlocks orderby entry.Value descending select entry).ToList();
                dataGridView3.DataSource = (from entry in bpIngots orderby entry.Value descending select entry).ToList();
                dataGridView1.Columns[0].HeaderText = "Component name";
                dataGridView2.Columns[0].HeaderText = "Block name";
                dataGridView3.Columns[0].HeaderText = "Resource name";
                dataGridView1.Columns[0].Width = 175;
                dataGridView2.Columns[0].Width = 175;
                dataGridView3.Columns[0].Width = 175;

            }
            catch (Exception ex)
            {
                if (ex is XmlException)
                {
                    MessageBox.Show("Make sure a .bp file was selected");
                }
                else if (ex is FileNotFoundException)
                {
                    MessageBox.Show("File was not selected");
                }
                else if(ex.Message == "NullDirectory")
                {
                    MessageBox.Show("Set space engineers game directory location");
                    button2_Click(sender, e);
                }
                else MessageBox.Show(ex.Message);
                return;
            }
        }

        public Dictionary<string, int> readXMLBlueprintBlocks(string file)
        {
            Dictionary<string, int> blockDict = new Dictionary<string, int>();
            string key;
            XmlDocument bp = new XmlDocument();

            bp.LoadXml(file);

            var blocks = bp.DocumentElement.SelectNodes("//CubeBlocks/MyObjectBuilder_CubeBlock/SubtypeName");

            foreach(XmlNode block in blocks)
            {
                key = block?.InnerText ?? "";
                if (key == "") continue;
                if (blockDict.ContainsKey(key)) blockDict[key]++;
                else blockDict.Add(key, 1);
            }

            return blockDict;
        }

        public Dictionary<string, Dictionary<string, int>> readXMLBlockInfo(string file, Dictionary<string, Dictionary<string, int>> blockDict)
        {
            Dictionary<string, int> compDict = new Dictionary<string, int>();
            int compCount;

            XmlDocument blocks = new XmlDocument();
            blocks.LoadXml(file);

            var blockSections = blocks.DocumentElement.SelectNodes("//Definition");

            foreach(XmlNode section in blockSections)
            {
                var blockName = section.SelectSingleNode(".//Id/SubtypeId")?.InnerText ??"";
                var components = section.SelectNodes(".//Components/Component");
                foreach(XmlElement component in components)
                {
                    var compName = component.GetAttribute("Subtype");
                    int.TryParse(component.GetAttribute("Count"), out compCount);

                    if (compDict.ContainsKey(compName)) compDict[compName] += compCount;
                    else compDict.Add(compName, compCount);
                }
                if (blockDict.ContainsKey(blockName)) continue;
                else blockDict.Add(blockName, new Dictionary<string, int>(compDict));
                compDict.Clear();
            }

            return blockDict;
        }

        public Dictionary<string, Dictionary<string, float>> readXMLComponentInfo(string file, Dictionary<string, Dictionary<string, float>> compDict)
        {
            Dictionary<string, float> ingotDict = new Dictionary<string, float>();
            float ingotCount;

            XmlDocument comps = new XmlDocument();
            comps.LoadXml(file);

            var compSections = comps.DocumentElement.SelectNodes("//Blueprint");

            foreach (XmlNode section in compSections)
            {
                var compName = "";
                var comp = section.SelectSingleNode(".//Result");
                if (comp == null || comp.Attributes["TypeId"].Value != "Component") continue;
                compName = comp.Attributes["SubtypeId"]?.Value ?? "";
                var ingots = section.SelectNodes(".//Prerequisites/Item");
                foreach (XmlElement ingot in ingots)
                {
                    var ingotName = ingot.GetAttribute("SubtypeId") + " " + ingot.GetAttribute("TypeId");
                    float.TryParse(ingot.GetAttribute("Amount"), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out ingotCount);

                    if (ingotDict.ContainsKey(ingotName)) ingotDict[ingotName] += ingotCount;
                    else ingotDict.Add(ingotName, ingotCount);
                }
                if (compDict.ContainsKey(compName)) continue;
                else compDict.Add(compName, new Dictionary<string, float>(ingotDict));
                ingotDict.Clear();
            }

            return compDict;
        }

        public Dictionary<string, int> getComponents(Dictionary<string, int> bpBlocks)
        {
            Dictionary<string, Dictionary<string, int>> blockDict = readBlocksData();
            Dictionary<string, int> comps = new Dictionary<string, int>();
            
            foreach (var bpBlock in bpBlocks)
            {
                foreach(var comp in blockDict[bpBlock.Key])
                {
                    if (comps.ContainsKey(comp.Key)) comps[comp.Key] += comp.Value * bpBlock.Value;
                    else comps.Add(comp.Key, comp.Value * bpBlock.Value);
                }
            }

            return comps;
        }

        public Dictionary<string, float> getIngots(Dictionary<string, int> bpComps)
        {
            Dictionary<string, Dictionary<string, float>> compDict = readCompsData();
            Dictionary<string, float> ingots = new Dictionary<string, float>();

            foreach (var bpComp in bpComps)
            {
                foreach (var ingot in compDict[bpComp.Key])
                {
                    if (ingots.ContainsKey(ingot.Key)) ingots[ingot.Key] += ingot.Value * bpComp.Value;
                    else ingots.Add(ingot.Key, ingot.Value * bpComp.Value);
                }
            }

            return ingots;
        }

        public void UpdateData()
        {
            Dictionary<string, Dictionary<string, int>> blockDict = new Dictionary<string, Dictionary<string, int>>();
            Dictionary<string, Dictionary<string, float>> compDict = new Dictionary<string, Dictionary<string, float>>();

            try
            {
                string path = readGameDir() + "\\Content\\Data\\CubeBlocks\\";

                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_Armor.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_Armor_2.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_Automation.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_Communications.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_Control.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_DecorativePack.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_DecorativePack2.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_Doors.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_Economy.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_Energy.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_Extras.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_Frostbite.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_Gravity.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_Interiors.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_LCDPanels.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_Lights.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_Logistics.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_Mechanical.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_Medical.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_Production.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_ScrapRacePack.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_SparksOfTheFuturePack.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_Symbols.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_Thrusters.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_Tools.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_Utility.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_Weapons.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_Wheels.sbc"), blockDict);
                readXMLBlockInfo(File.ReadAllText(path + "CubeBlocks_Windows.sbc"), blockDict);

                readXMLComponentInfo(File.ReadAllText(readGameDir() + "\\Content\\Data\\Blueprints.sbc"), compDict);

                string output = JsonConvert.SerializeObject(blockDict, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(rootDir + "../Data/Blocks.json", output);

                output = JsonConvert.SerializeObject(compDict, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(rootDir + "../Data/Components.json", output);

                MessageBox.Show("Blocks and Components info updated");
            }
            catch (DirectoryNotFoundException)
            {
                MessageBox.Show("Couldnt update blocks info. Make sure your game directory setting is correct.");
            }
            catch (Exception ex)
            {
                if (ex.Message == "NullDirectory")
                {
                    MessageBox.Show("Set space engineers game directory location");
                }
            }
        }

        public Dictionary<string, Dictionary<string, int>> readBlocksData()
        {
            Dictionary<string, Dictionary<string, int>> blockDict = new Dictionary<string, Dictionary<string, int>>();
            JObject blocks = JObject.Parse(File.ReadAllText(rootDir + "../Data/Blocks.json"));

            foreach (var block in blocks)
            {
                blockDict.Add(block.Key, block.Value.ToObject<Dictionary<string, int>>());
            }

            return blockDict;
        }

        public Dictionary<string, Dictionary<string, float>> readCompsData()
        {
            Dictionary<string, Dictionary<string, float>> compDict = new Dictionary<string, Dictionary<string, float>>();
            JObject comps = JObject.Parse(File.ReadAllText(rootDir + "../Data/Components.json"));

            foreach (var comp in comps)
            {
                compDict.Add(comp.Key, comp.Value.ToObject<Dictionary<string, float>>());
            }

            return compDict;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string s = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Valve\Steam", "InstallPath", "");
            if (s == "") s = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath", "");
            else
            {
                if (Directory.Exists(s + "../steamapps/common/SpaceEngineers"))
                    SaveDir(s + "../steamapps/common/SpaceEngineers");
                else
                {
                    AcfReader acf = new AcfReader(s + "../steamapps/libraryfolders.vdf");
                    acf.CheckIntegrity();
                    ACF_Struct acfStruct = acf.ACFFileToStruct();
                    var folders = acfStruct.SubACF.Values.First().SubItems;
                    foreach(var folder in folders)
                    {
                        if (Directory.Exists(folder.Value + "../steamapps/common/SpaceEngineers"))
                        {
                            SaveDir(folder.Value + "../steamapps/common/SpaceEngineers");
                            break;
                        }
                    }
                }
                MessageBox.Show("Detected SE game directory at: " + s + "\\steamapps\\common\\SpaceEngineers");
                return;
            }
            if (s == "") MessageBox.Show("Couldnt detect game directory, set SE location manually");

            folderBrowserDialog1.ShowDialog();
            SaveDir(folderBrowserDialog1.SelectedPath);
        }

        public void SaveDir(string dir)
        {
            using (FileStream fs = File.Create(rootDir + "../Data/SEdir.txt"))
            {
                Byte[] path = new UTF8Encoding(true).GetBytes(dir);
                fs.Write(path, 0, path.Length);
            }
        }

        public string readGameDir()
        {
            string s = "";
            using (StreamReader sr = File.OpenText(rootDir + "../Data/SEdir.txt"))
            {
                s = sr.ReadLine();
            }
            if (s == "")
            {
                throw new Exception("NullDirectory");
            }
            return s;
        }

        private void button3_Click(object sender, EventArgs e)
        {
                UpdateData();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            MessageBox.Show("SEBlueprintCalc v1 by Guzuu\nReport any issues by a discord DM:\nDizzy#5556 or 186104843478368256\nPage: https://github.com/Guzuu/SEBlueprintCalc");
        }
    }
}
