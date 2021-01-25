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

namespace SEBlueprintCalc
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            UpdateBlocksData();
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
                Dictionary<string, int> bp = readXMLBlueprint(bpFile);


                dataGridView1.DataSource = getComponents(bp).ToList();
                dataGridView2.DataSource = bp.ToList();
                dataGridView1.Columns[0].HeaderText = "Component name";
                dataGridView2.Columns[0].HeaderText = "Block name";
                dataGridView1.Columns[0].Width = 175;
                dataGridView2.Columns[0].Width = 175;
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

        public Dictionary<string, int> readXMLBlueprint(string file)
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();
            string key;
            XmlDocument bp = new XmlDocument();

            bp.LoadXml(file);

            var blocks = bp.DocumentElement.SelectNodes("//CubeBlocks/MyObjectBuilder_CubeBlock/SubtypeName");

            foreach(XmlNode block in blocks)
            {
                key = block?.InnerText ?? "";
                if (key == "") continue;
                if (dict.ContainsKey(key)) dict[key]++;
                else dict.Add(key, 1);
            }

            return dict;
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

        public void UpdateBlocksData()
        {
            Dictionary<string, Dictionary<string, int>> blockDict = new Dictionary<string, Dictionary<string, int>>();

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

                string output = JsonConvert.SerializeObject(blockDict, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(rootDir + "../Data/Blocks.json", output);
                MessageBox.Show("Blocks info updated");
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

        private void button2_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.ShowDialog();
            using (FileStream fs = File.Create(rootDir + "../Data/SEdir.txt"))
            {
                Byte[] path = new UTF8Encoding(true).GetBytes(folderBrowserDialog1.SelectedPath);
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
            if (s == null)
            {
                throw new Exception("NullDirectory");
            }
            return s;
        }

        private void button3_Click(object sender, EventArgs e)
        {
                UpdateBlocksData();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            MessageBox.Show("SEBlueprintCalc v1 by Guzuu\nReport any issues by a discord DM:\nDizzy#5556 or 186104843478368256\nPage: https://github.com/Guzuu/SEBlueprintCalc");
        }
    }
}
