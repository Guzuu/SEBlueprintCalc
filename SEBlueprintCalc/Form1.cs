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
        public struct ItemData<T>
        {
            public string IconPath { get; set; }
            public Dictionary<string, T> Cost { get; set; }

            public ItemData(string DDSPath, Dictionary<string, T> Cost)
            {
                this.IconPath = DDSPath;
                this.Cost = new Dictionary<string, T>(Cost);
            }
        }
        
        public Form1()
        {
            InitializeComponent();
            UpdateData();
            dataGridView1.RowTemplate.Height = 50;
            dataGridView2.RowTemplate.Height = 50;
            dataGridView3.RowTemplate.Height = 50;
        }

        public string rootDir = Directory.GetCurrentDirectory();

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.FileName = ".bp file";
            openFileDialog1.ShowDialog();

            try
            {
                var bpFile = File.ReadAllText(openFileDialog1.FileName);
                pictureBox1.Image = Image.FromFile(Path.GetDirectoryName(openFileDialog1.FileName) + "\\thumb.png");
                label1.Text = Path.GetFileName(Path.GetDirectoryName(openFileDialog1.FileName));
                MySortableBindingList<DGVItem<int>> bpBlocks = readXMLBlueprintBlocks(bpFile);
                MySortableBindingList<DGVItem<int>> bpComps = getComponents(bpBlocks);
                MySortableBindingList<DGVItem<float>> bpIngots = getIngots(bpComps);

                dataGridView2.DataSource = bpBlocks;
                dataGridView1.DataSource = bpComps;
                dataGridView3.DataSource = bpIngots;

                dataGridView2.Columns[0].Width = 50;
                dataGridView2.Columns[1].Width = 175;
                dataGridView2.Columns[2].Width = 50;
                dataGridView1.Columns[0].Width = 50;
                dataGridView1.Columns[1].Width = 150;
                dataGridView1.Columns[2].Width = 75;
                dataGridView3.Columns[0].Width = 50;
                dataGridView3.Columns[1].Width = 125;
                dataGridView3.Columns[2].Width = 100;
                dataGridView2.Columns[1].HeaderText = "Block name";
                dataGridView1.Columns[1].HeaderText = "Component name";
                dataGridView3.Columns[1].HeaderText = "Ingot name";
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

        public Dictionary<string, ItemData<int>> readXMLBlockInfo(string file, Dictionary<string, ItemData<int>> blockDict)
        {
            Dictionary<string, int> compDict = new Dictionary<string, int>();
            int compCount;
            XmlDocument blocks = new XmlDocument();
            blocks.LoadXml(file);

            var blockSections = blocks.DocumentElement.SelectNodes("//Definition");

            foreach (XmlNode section in blockSections)
            {
                var blockName = section.SelectSingleNode(".//Id/SubtypeId")?.InnerText ?? "";
                if (blockName == "")
                {
                    blockName = section.SelectSingleNode(".//Id/TypeId")?.InnerText ?? "";
                }
                var IconPath = section.SelectSingleNode(".//Icon")?.InnerText ?? "";
                var components = section.SelectNodes(".//Components/Component");
                foreach (XmlElement component in components)
                {
                    var compName = component.GetAttribute("Subtype");
                    int.TryParse(component.GetAttribute("Count"), out compCount);

                    if (compDict.ContainsKey(compName)) compDict[compName] += compCount;
                    else compDict.Add(compName, compCount);
                }
                if (blockDict.ContainsKey(blockName)) continue;
                else blockDict.Add(blockName, new ItemData<int>(IconPath, compDict));
                compDict.Clear();
            }
            return blockDict;
        }

        public Dictionary<string, ItemData<float>> readXMLComponentInfo(string file, Dictionary<string, ItemData<float>> compDict)
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
                var IconPath = section.SelectSingleNode(".//Icon")?.InnerText ?? "";
                var ingots = section.SelectNodes(".//Prerequisites/Item");
                foreach (XmlElement ingot in ingots)
                {
                    var ingotName = ingot.GetAttribute("SubtypeId") + " " + ingot.GetAttribute("TypeId");
                    float.TryParse(ingot.GetAttribute("Amount"), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out ingotCount);
                    ingotCount /= 3;

                    if (ingotDict.ContainsKey(ingotName)) ingotDict[ingotName] += ingotCount;
                    else ingotDict.Add(ingotName, ingotCount);
                }
                if (compDict.ContainsKey(compName)) continue;
                else compDict.Add(compName, new ItemData<float>(IconPath, ingotDict));
                ingotDict.Clear();
            }
            return compDict;
        }

        public Dictionary<string, ItemData<float>> readXMLIngotInfo(string file, Dictionary<string, ItemData<float>> ingotDict)
        {
            Dictionary<string, float> oreDict = new Dictionary<string, float>();
            float oreCount;

            XmlDocument ingots = new XmlDocument();
            ingots.LoadXml(file);

            var ingotSections = ingots.DocumentElement.SelectNodes("//Blueprint");

            foreach (XmlNode section in ingotSections)
            {
                var ingotName = "";
                var ingot = section.SelectSingleNode(".//Prerequisites/Item");
                if (ingot == null || ingot.Attributes["TypeId"].Value != "Ore") continue;
                ingotName = ingot.Attributes["SubtypeId"]?.Value ?? "";
                ingotName += " Ingot";
                var IconPath = section.SelectSingleNode(".//Icon")?.InnerText ?? "";

                if (ingotName == "Stone Ingot" && !ingotDict.ContainsKey("Stone Ingot"))
                {
                    oreDict.Add("Stone Ore", 1);
                    ingotDict.Add(ingotName, new ItemData<float>(IconPath, oreDict));
                    oreDict.Clear();
                    continue;
                }
                var ores = section.SelectNodes(".//Result");
                foreach (XmlElement ore in ores)
                {
                    var oreName = ore.GetAttribute("SubtypeId") + " Ore";
                    float.TryParse(ore.GetAttribute("Amount"), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out oreCount);

                    if (oreDict.ContainsKey(oreName)) continue;
                    else oreDict.Add(oreName, oreCount);
                }
                if (ingotDict.ContainsKey(ingotName)) continue;
                else ingotDict.Add(ingotName, new ItemData<float>(IconPath, oreDict));
                oreDict.Clear();
            }
            return ingotDict;
        }

        public MySortableBindingList<DGVItem<int>> readXMLBlueprintBlocks(string file)
        {
            MySortableBindingList<DGVItem<int>> bpBlocks = new MySortableBindingList<DGVItem<int>>();
            var iconPaths = readBlocksIconsData();
            string name, partialPath = readGameDir() + "\\Content\\";
            XmlDocument bp = new XmlDocument();

            bp.LoadXml(file);

            var blocks = bp.DocumentElement.SelectNodes("//CubeGrids/CubeGrid/CubeBlocks/MyObjectBuilder_CubeBlock/SubtypeName");

            foreach(XmlNode block in blocks)
            {
                name = block?.InnerText ?? "";
                if (name == "")
                {
                    name = block.ParentNode.Attributes[0]?.InnerText ?? "";
                    name = name.Substring(16);
                }
                if (name == "") continue;
                DGVItem<int> foundBlock = bpBlocks.FirstOrDefault(p => p.Name == name);
                if (foundBlock != null) foundBlock.Count++;
                else bpBlocks.Add(new DGVItem<int>(name, 1, partialPath+iconPaths[name]));
            }
            return bpBlocks;
        }

        public MySortableBindingList<DGVItem<int>> getComponents(MySortableBindingList<DGVItem<int>> bpBlocks)
        {
            Dictionary<string, Dictionary<string, int>> blockDict = readBlocksData();
            Dictionary<string, string> iconPaths = readCompsIconsData();
            string partialPath = readGameDir() + "\\Content\\";
            MySortableBindingList<DGVItem<int>> comps = new MySortableBindingList<DGVItem<int>>();

            foreach (var bpBlock in bpBlocks)
            {
                foreach(var comp in blockDict[bpBlock.Name])
                {
                    DGVItem<int> foundComp = comps.FirstOrDefault(p => p.Name == comp.Key);
                    if (foundComp != null) foundComp.Count += comp.Value * bpBlock.Count;
                    else comps.Add(new DGVItem<int>(comp.Key, comp.Value * bpBlock.Count, partialPath + iconPaths[comp.Key]));
                }
            }
            return comps;
        }

        public MySortableBindingList<DGVItem<float>> getIngots(MySortableBindingList<DGVItem<int>> bpComps)
        {
            Dictionary<string, Dictionary<string, float>> compDict = readCompsData();
            Dictionary<string, string> iconPaths = readIngotsIconsData();
            string partialPath = readGameDir() + "\\Content\\";
            MySortableBindingList<DGVItem<float>> ingots = new MySortableBindingList<DGVItem<float>>();

            foreach (var bpComp in bpComps)
            {
                foreach (var ingot in compDict[bpComp.Name])
                {
                    DGVItem<float> foundIngot = ingots.FirstOrDefault(p => p.Name == ingot.Key);
                    if (foundIngot != null) foundIngot.Count += ingot.Value * bpComp.Count;
                    else ingots.Add(new DGVItem<float>(ingot.Key, ingot.Value * bpComp.Count, partialPath+iconPaths[ingot.Key]));
                }
            }
            return ingots;
        }

        public void UpdateData()
        {
            Dictionary<string, ItemData<int>> blockDict = new Dictionary<string, ItemData<int>>();
            Dictionary<string, ItemData<float>> compDict = new Dictionary<string, ItemData<float>>();
            Dictionary<string, ItemData<float>> ingotDict = new Dictionary<string, ItemData<float>>();

            try
            {
                string path = readGameDir() + "\\Content\\Data\\CubeBlocks\\";

                foreach (string file in Directory.EnumerateFiles(path, "*.sbc"))
                {
                    readXMLBlockInfo(File.ReadAllText(file), blockDict);
                }
                readXMLComponentInfo(File.ReadAllText(readGameDir() + "\\Content\\Data\\Blueprints.sbc"), compDict);
                readXMLIngotInfo(File.ReadAllText(readGameDir() + "\\Content\\Data\\Blueprints.sbc"), ingotDict);

                string output = JsonConvert.SerializeObject(blockDict, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(rootDir + "../Data/Blocks.json", output);

                output = JsonConvert.SerializeObject(compDict, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(rootDir + "../Data/Components.json", output);

                output = JsonConvert.SerializeObject(ingotDict, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(rootDir + "../Data/Ingots.json", output);

                MessageBox.Show("Data updated");
            }
            catch (DirectoryNotFoundException)
            {
                MessageBox.Show("Couldnt update data. Make sure your game directory setting is correct.");
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
                blockDict.Add(block.Key, block.Value.ToObject<ItemData<int>>().Cost);
            }

            return blockDict;
        }

        public Dictionary<string, string> readBlocksIconsData()
        {
            Dictionary<string, string> blockIconDict = new Dictionary<string, string>();
            JObject blocks = JObject.Parse(File.ReadAllText(rootDir + "../Data/Blocks.json"));

            foreach (var block in blocks)
            {
                blockIconDict.Add(block.Key, block.Value.ToObject<ItemData<int>>().IconPath);
            }

            return blockIconDict;
        }

        public Dictionary<string, Dictionary<string, float>> readCompsData()
        {
            Dictionary<string, Dictionary<string, float>> compDict = new Dictionary<string, Dictionary<string, float>>();
            JObject comps = JObject.Parse(File.ReadAllText(rootDir + "../Data/Components.json"));

            foreach (var comp in comps)
            {
                compDict.Add(comp.Key, comp.Value.ToObject<ItemData<float>>().Cost);
            }

            return compDict;
        }

        public Dictionary<string, string> readCompsIconsData()
        {
            Dictionary<string, string> compIconDict = new Dictionary<string, string>();
            JObject comps = JObject.Parse(File.ReadAllText(rootDir + "../Data/Components.json"));

            foreach (var comp in comps)
            {
                compIconDict.Add(comp.Key, comp.Value.ToObject<ItemData<float>>().IconPath);
            }

            return compIconDict;
        }

        public Dictionary<string, string> readIngotsIconsData()
        {
            Dictionary<string, string> ingotIconDict = new Dictionary<string, string>();
            JObject ingots = JObject.Parse(File.ReadAllText(rootDir + "../Data/Ingots.json"));

            foreach (var ingot in ingots)
            {
                ingotIconDict.Add(ingot.Key, ingot.Value.ToObject<ItemData<float>>().IconPath);
            }

            return ingotIconDict;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string s = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Valve\Steam", "InstallPath", "");
            if (s == "") s = (string)Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam", "InstallPath", "");
            if (s != "")
            {
                if (Directory.Exists(s + "../steamapps/common/SpaceEngineers"))
                {
                    MessageBox.Show("Detected SE game directory at: " + s + "\\steamapps\\common\\SpaceEngineers");
                    SaveDir(s + "../steamapps/common/SpaceEngineers");
                }
                else
                {
                    AcfReader acf = new AcfReader(s + "../steamapps/libraryfolders.vdf");
                    acf.CheckIntegrity();
                    ACF_Struct acfStruct = acf.ACFFileToStruct();
                    var folders = acfStruct.SubACF.Values.First().SubACF;
                    foreach (var folder in folders)
                    {
                        foreach (var subItem in folder.Value.SubItems)
                        {
                            if (Directory.Exists(subItem.Value + "../steamapps/common/SpaceEngineers"))
                            {
                                MessageBox.Show("Detected SE game directory at: " + subItem.Value + "\\steamapps\\common\\SpaceEngineers");
                                SaveDir(subItem.Value + "../steamapps/common/SpaceEngineers");
                                break;
                            }
                        }
                    }
                }
                return;
            }
            else MessageBox.Show("Couldnt detect steam directory, set SE location manually");
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
            MessageBox.Show("SEBlueprintCalc v3 by Guzuu\nReport any issues by a discord DM:\nDizzy#5556 or 186104843478368256\nPage: https://github.com/Guzuu/SEBlueprintCalc \nCtrl+C to copy contents");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog();
            SaveDir(folderBrowserDialog1.SelectedPath);
        }
    }
}
