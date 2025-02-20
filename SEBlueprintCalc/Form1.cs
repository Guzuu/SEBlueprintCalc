﻿using System;
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
using System.Configuration;
using System.Diagnostics;

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
            if (int.TryParse(ConfigurationManager.AppSettings["RowHeight"], out int tempRH)) rowHeight = tempRH;
            if (float.TryParse(ConfigurationManager.AppSettings["FontSize"], out float tempFS)) fontSize = tempFS;
            UpdateData();
            dataGridView1.RowTemplate.Height = rowHeight;
            dataGridView2.RowTemplate.Height = rowHeight;
            dataGridView3.RowTemplate.Height = rowHeight;
            dataGridView1.DefaultCellStyle.Font = new Font(FontFamily.GenericSansSerif, fontSize);
            dataGridView2.DefaultCellStyle.Font = new Font(FontFamily.GenericSansSerif, fontSize);
            dataGridView3.DefaultCellStyle.Font = new Font(FontFamily.GenericSansSerif, fontSize);
            numericUpDown2.Value = rowHeight;
            numericUpDown1.Value = (decimal)fontSize;
            richTextBox1.LinkClicked += RichTextBox_LinkClicked;
        }

        public string rootDir = "../"; //Directory.GetCurrentDirectory();
        public int rowHeight = 50;
        public float fontSize = 9.75f;
        MySortableBindingList<DGVItem<int>> bpBlocks;
        MySortableBindingList<DGVItem<int>> bpComps;
        MySortableBindingList<DGVItem<float>> bpIngots;

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1 = new OpenFileDialog
            {
                Filter = "Space Engineers Blueprint Files (*.sbc)|*.sbc|All Files (*.*)|*.*",
                Title = "Select a .sbc File",
                DefaultExt = "sbc",
            };

            // Set default path
            string defaultPath = Environment.ExpandEnvironmentVariables("%appdata%\\SpaceEngineers\\Blueprints\\local");
            if (Directory.Exists(defaultPath))
            {
                openFileDialog1.InitialDirectory = defaultPath;
            }

            // Show the dialog
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var bpFile = File.ReadAllText(openFileDialog1.FileName);
                    var path = Path.GetDirectoryName(openFileDialog1.FileName);
                    if (File.Exists(path + "\\thumb.png"))
                    {
                        pictureBox1.Image = Image.FromFile(path + "\\thumb.png");
                        label1.Text = Path.GetFileName(path);
                    }
                    else
                    {
                        pictureBox1.Image = null;
                        label1.Text = "Blueprint";
                    }
                    bpBlocks = readXMLBlueprintBlocks(bpFile);
                    bpComps = getComponents(bpBlocks);
                    bpIngots = getIngots(bpComps);

                    dataGridView2.DataSource = bpBlocks;
                    dataGridView1.DataSource = bpComps;
                    dataGridView3.DataSource = bpIngots;
                    ((DataGridViewImageColumn)dataGridView1.Columns[0]).ImageLayout = DataGridViewImageCellLayout.Zoom;
                    ((DataGridViewImageColumn)dataGridView2.Columns[0]).ImageLayout = DataGridViewImageCellLayout.Zoom;
                    ((DataGridViewImageColumn)dataGridView3.Columns[0]).ImageLayout = DataGridViewImageCellLayout.Zoom;

                    dataGridView2.Columns[0].FillWeight = 100;
                    dataGridView2.Columns[1].FillWeight = 350;
                    dataGridView2.Columns[2].FillWeight = 100;
                    dataGridView1.Columns[0].FillWeight = 100;
                    dataGridView1.Columns[1].FillWeight = 300;
                    dataGridView1.Columns[2].FillWeight = 150;
                    dataGridView3.Columns[0].FillWeight = 100;
                    dataGridView3.Columns[1].FillWeight = 250;
                    dataGridView3.Columns[2].FillWeight = 200;

                    dataGridView2.Columns[1].HeaderText = "Block name";
                    dataGridView1.Columns[1].HeaderText = "Component name";
                    dataGridView3.Columns[1].HeaderText = "Ingot name";
                }
                catch (Exception ex)
                {
                    if (ex is XmlException)
                    {
                        MessageBox.Show(this, "Make sure an .sbc file was selected", "Info");
                    }
                    else if (ex is FileNotFoundException)
                    {
                        MessageBox.Show(this, "File was not selected", "Info");
                    }
                    else if (ex.Message == "NullDirectory")
                    {
                        MessageBox.Show(this, "Set space engineers game directory location", "Warning");
                        button2_Click(sender, e);
                    }
                    else MessageBox.Show(this, ex.Message, "Error");
                    return;
                }
            }
            else
            {
                MessageBox.Show(this, "No file selected.", "Info");
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
                var prototech = false;
                var comp = section.SelectSingleNode(".//Result");
                if (comp == null || comp.Attributes["TypeId"].Value != "Component") continue;
                compName = comp.Attributes["SubtypeId"]?.Value ?? "";
                var IconPath = section.SelectSingleNode(".//Icon")?.InnerText ?? "";

                //Fix, for a SE blueprints.sbc file containing wrong icon path for prototech frame recipe
                if (compName == "PrototechFrame" && IconPath.Substring(IconPath.LastIndexOf('\\') + 1) == "PrototechPanel_Component.dds")
                {
                    IconPath = IconPath.Substring(0, IconPath.LastIndexOf('\\') + 1) + "PrototechFrame.dds";
                }
                if (compName.Contains("Prototech"))
                {
                    prototech = true;
                }

                var ingots = section.SelectNodes(".//Prerequisites/Item");
                foreach (XmlElement ingot in ingots)
                {
                    var ingotName = ingot.GetAttribute("SubtypeId"); //+ " " + ingot.GetAttribute("TypeId");
                    float.TryParse(ingot.GetAttribute("Amount"), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out ingotCount);

                    //For some reason in files - recipe for standard blocks are exactly three times that we see in game. Why i dont know but to make matters worse, Newly added ProtoTech is NOT done the same way.
                    if (!prototech)
                    {
                        ingotCount /= 3;
                    }

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
                if (ingot == null || ingot.Attributes["TypeId"].Value != "Ore" && ingot.Attributes["SubtypeId"].Value != "PrototechScrap") continue;
                ingotName = ingot.Attributes["SubtypeId"]?.Value ?? "";
                //ingotName += " Ingot";

                var IconPath = section.SelectSingleNode(".//Icon")?.InnerText ?? "";

                //Fix, for a SE blueprints.sbc file containing wrong icon path for prototech scrap recipe
                if (ingotName == "PrototechScrap" && IconPath.Substring(IconPath.LastIndexOf('\\') + 1) == "PrototechPanel_Component.dds")
                {
                    IconPath = IconPath.Substring(0, IconPath.LastIndexOf('\\') + 1) + "ScrapPrototechComponent.dds";
                }

                if (ingotName == "Stone" && !ingotDict.ContainsKey("Stone"))
                {
                    oreDict.Add("Stone", 1);
                    ingotDict.Add(ingotName, new ItemData<float>(IconPath, oreDict));
                    oreDict.Clear();
                    continue;
                }
                var ores = section.SelectNodes(".//Result");
                foreach (XmlElement ore in ores)
                {
                    var oreName = ore.GetAttribute("SubtypeId");
                    if (oreName != "PrototechScrap") oreName +=" Ore";
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
            bool mod = false;
            string name, partialPath = readGameDir() + "\\Content\\";
            XmlDocument bp = new XmlDocument();

            bp.LoadXml(file);

            var blocks = bp.DocumentElement.SelectNodes("//CubeGrids/CubeGrid/CubeBlocks/MyObjectBuilder_CubeBlock/SubtypeName");

            foreach (XmlNode block in blocks)
            {
                name = block?.InnerText ?? "";
                if (name == "")
                {
                    name = block.ParentNode.Attributes[0]?.InnerText ?? "";
                    name = name.Substring(16);
                }
                if (name == "") continue;
                if (!iconPaths.ContainsKey(name))
                {
                    mod = true;
                    continue;
                }
                DGVItem<int> foundBlock = bpBlocks.FirstOrDefault(p => p.Name == name);
                if (foundBlock != null) foundBlock.Count++;
                else bpBlocks.Add(new DGVItem<int>(name, 1, partialPath + iconPaths[name]));
            }
            if (mod) MessageBox.Show(this, "Some unrecognized blocks have been ignored", "Warning");
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
                if (blockDict.ContainsKey(bpBlock.Name))
                {
                    foreach (var comp in blockDict[bpBlock.Name])
                    {
                        DGVItem<int> foundComp = comps.FirstOrDefault(p => p.Name == comp.Key);
                        if (foundComp != null) foundComp.Count += comp.Value * bpBlock.Count;
                        else comps.Add(new DGVItem<int>(comp.Key, comp.Value * bpBlock.Count, partialPath + iconPaths[comp.Key]));
                    }
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
                if (bpComp.Name == "ZoneChip" || bpComp.Name == "PrototechFrame") continue;
                foreach (var ingot in compDict[bpComp.Name])
                {
                    DGVItem<float> foundIngot = ingots.FirstOrDefault(p => p.Name == ingot.Key);
                    if (foundIngot != null) foundIngot.Count += ingot.Value * bpComp.Count;
                    else ingots.Add(new DGVItem<float>(ingot.Key, ingot.Value * bpComp.Count, partialPath + iconPaths[ingot.Key])); //+ iconPaths[ingot.Key]
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

                MessageBox.Show(this, "Data updated", "Info");
            }
            catch (DirectoryNotFoundException)
            {
                MessageBox.Show(this, "Couldnt update data. Make sure your game directory setting is correct.", "Error");
            }
            catch (Exception ex)
            {
                if (ex.Message == "NullDirectory")
                {
                    MessageBox.Show(this, "Set space engineers game directory location", "Warning");
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
                if (Directory.Exists(s + "/steamapps/common/SpaceEngineers"))
                {
                    MessageBox.Show(this, "Detected SE game directory at: " + s + "\\steamapps\\common\\SpaceEngineers", "Info");
                    SaveDir(s + "/steamapps/common/SpaceEngineers");
                }
                else
                {
                    AcfReader acf = new AcfReader(s + "/steamapps/libraryfolders.vdf");
                    acf.CheckIntegrity();
                    ACF_Struct acfStruct = acf.ACFFileToStruct();
                    var folders = acfStruct.SubACF.Values.First().SubACF;
                    foreach (var folder in folders)
                    {
                        foreach (var subItem in folder.Value.SubItems)
                        {
                            if (Directory.Exists(subItem.Value + "/steamapps/common/SpaceEngineers"))
                            {
                                MessageBox.Show(this, "Detected SE game directory at: " + subItem.Value + "\\steamapps\\common\\SpaceEngineers", "Info");
                                SaveDir(subItem.Value + "/steamapps/common/SpaceEngineers");
                                break;
                            }
                        }
                    }
                }
                return;
            }
            else MessageBox.Show(this, "Couldnt detect steam directory, set SE location manually", "Error");
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

        private void button5_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK) SaveDir(folderBrowserDialog1.SelectedPath);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (bpComps == null || bpIngots == null) return;
            string Isy = "";
            foreach (var item in bpComps)
            {
                Isy += "Component/" + item.Name + "=" + string.Format("{0:F0}", item.Count) + "\n";
            }
            foreach (var item in bpIngots)
            {
                Isy += "Ingot/" + item.Name + "=" + string.Format("{0:F0}", Math.Ceiling(item.Count)) + "\n";
            }
            Clipboard.SetText(Isy);
            button6.Text = "Copied";
        }

        private void button6_MouseLeave(object sender, EventArgs e)
        {
            button6.Text = "Isy's IM";
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (numericUpDown2.Value != 0)
            {
                rowHeight = (int)numericUpDown2.Value;

                dataGridView1.RowTemplate.Height = rowHeight;
                dataGridView2.RowTemplate.Height = rowHeight;
                dataGridView3.RowTemplate.Height = rowHeight;
                foreach(DataGridViewRow r in dataGridView1.Rows)
                {
                    r.Height = rowHeight;
                }
                foreach (DataGridViewRow r in dataGridView2.Rows)
                {
                    r.Height = rowHeight;
                }
                foreach (DataGridViewRow r in dataGridView3.Rows)
                {
                    r.Height = rowHeight;
                }
                
                if(config.AppSettings.Settings["RowHeight"] != null)
                {
                    config.AppSettings.Settings["RowHeight"].Value = rowHeight.ToString();
                }
                else config.AppSettings.Settings.Add("RowHeight", rowHeight.ToString());
            }
            if(numericUpDown1.Value != 0)
            {
                fontSize = (int)numericUpDown1.Value;

                dataGridView1.DefaultCellStyle.Font = new Font(FontFamily.GenericSansSerif, fontSize);
                dataGridView2.DefaultCellStyle.Font = new Font(FontFamily.GenericSansSerif, fontSize);
                dataGridView3.DefaultCellStyle.Font = new Font(FontFamily.GenericSansSerif, fontSize);

                if (config.AppSettings.Settings["FontSize"] != null)
                {
                    config.AppSettings.Settings["FontSize"].Value = fontSize.ToString();
                }
                else config.AppSettings.Settings.Add("FontSize", fontSize.ToString());
            }

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        private void RichTextBox_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            try
            {
                // Open the link in the default browser
                Process.Start(new ProcessStartInfo(e.LinkText) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                // Handle errors (e.g., invalid URL)
                MessageBox.Show(this, $"Failed to open link: {ex.Message}", "Error");
            }
        }
    }
}
