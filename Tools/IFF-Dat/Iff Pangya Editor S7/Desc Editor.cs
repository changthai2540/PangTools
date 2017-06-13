﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Iff_Pangya_Editor_S7
{
    public partial class Desc_Editor : Form
    {
        public class DescStock
        {
            public int Index;
            public uint ID;
            public string Texte;
        }

        List<DescStock> DescListing;
        List<DescList> DescListMem;
        public Desc_Editor()
        {
            InitializeComponent();
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Pangya IFF Desc (Desc*.iff)|Desc*.iff",
                Title = "Open IFF Desc"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                DescListing = new List<DescStock>();
                DescListMem = new List<DescList>();
                this.lstStrings.Items.Clear();
                DescListMem = DescList.LoadDescFile(dialog.FileName);
                int i = 0;
                foreach (DescList record in this.DescListMem)
                {
                    DescStock item = new DescStock
                    {
                        Texte = record.Description.ToString(),
                        ID = record.IdObject,
                        Index = i
                    };
                    this.DescListing.Add(item);
                    i++;
                }
                UpdateStringList();
            }
        }

        private void UpdateStringList()
        {
            if (txtFilter.Text == "")
            {
                this.lstStrings.Items.Clear();
                foreach (DescStock str in this.DescListing)
                {
                    ListViewItem item = new ListViewItem(str.ID.ToString())
                    {
                        Tag = str.Index
                    };
                    this.lstStrings.Items.Add(item);
                }
            }
            else
            {
                this.lstStrings.Items.Clear();
                foreach (DescStock str in this.DescListing)
                {
                    ListViewItem item = new ListViewItem(str.ID.ToString())
                    {
                        Tag = str.Index
                    };
                    bool found = str.ID.ToString().Contains(txtFilter.Text);

                    if (found == true)
                        this.lstStrings.Items.Add(item);
                }
            }
        }
        private void lstStrings_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.lstStrings.SelectedItems.Count > 0)
            {

                DescStock SelectDesc = this.DescListing[(int)this.lstStrings.SelectedItems[0].Tag];
                this.txtString.Text = SelectDesc.Texte.Replace("\n", "\r\n");
                this.ObjectId.Text = SelectDesc.ID.ToString();
                this.CaractNum.Text = $"( {this.txtString.Text.Length} / {DescList.DescriptionLen})";
            }
        }

        private void txtString_TextChanged(object sender, EventArgs e)
        {
            this.CaractNum.Text = $"( {this.txtString.Text.Length} / {DescList.DescriptionLen})";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DescStock item = new DescStock
            {
                Texte = this.txtString.Text.Replace("\r\n", "\n"),
                ID = Convert.ToUInt32(ObjectId.Text),
                Index = Convert.ToInt32(DescListing.Last().Index + 1)
            };
            this.DescListing.Add(item);
            UpdateStringList();
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            if (txtString.Text != null)
            {

                DescListing[(int)this.lstStrings.SelectedItems[0].Tag].Texte = this.txtString.Text.Replace("\r\n", "\n");
                UpdateStringList();
            }
            UpdateStringList();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Title = "Save Pangya IFF Desc",
                Filter = "Pangya IFF File (*.iff)|*.iff"
            };
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (DescList.SaveDescFile(dialog.FileName, this.DescListing))
                {
                    MessageBox.Show("The Desc IFF have been saved!");
                }
                else
                {
                    MessageBox.Show("Error while writing the file. Please Try Again.");
                }
            }
        }
    }

    public class DescList
    {
        //Value using
        public string Description = "";
        public uint   IdObject;
        // Core value for read/rw
        public static int IdObjetlen = 4;
        public static int DescriptionLen = 0x200; //512
        public static int TotalLen = 0x204;

        public static List<DescList> LoadDescFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return new List<DescList>();
            }
            List<DescList> list = new List<DescList>();
            using (BinaryReader reader = new BinaryReader(File.Open(fileName, FileMode.Open, FileAccess.Read), IffFile.FileEncoding(IffFile.IFF_REGION.Default)))
            {
                IffFile file = new IffFile();
                ushort numberOfRecords = file.GetNumberOfRecords(reader);
                file.JumpToFirstRecord(reader);
                if (file.CheckMagicNumber(reader))
                {
                    for (int i = 0; i < numberOfRecords; i++)
                    {
                        long position = reader.BaseStream.Position;
                        DescList item = new DescList
                        {
                            IdObject = reader.ReadUInt32()
                        };
                        position += IdObjetlen;
                        item.Description = IffFile.FileEncoding(IffFile.IFF_REGION.Default).GetString(reader.ReadBytes(DescriptionLen));
                        position += DescriptionLen;
                        list.Add(item);
                    }
                    reader.Close();
                    return list;
                }
                return new List<DescList>();
            } 
       }

        public static bool SaveDescFile(string fileName, List<Desc_Editor.DescStock> descriptionList)
        {
            BinaryWriter writer = new BinaryWriter(File.Open(fileName, FileMode.Create, FileAccess.Write), IffFile.FileEncoding(IffFile.IFF_REGION.Default));
            IffFile file = new IffFile
            {
                ObjectsInFile = ushort.Parse(descriptionList.Count.ToString())
            };
            file.WriteIffFileHeader(writer);
            file.StubRecords(writer, TotalLen, descriptionList.Count);
            file.JumpToFirstRecord(writer);
            foreach (Desc_Editor.DescStock record in descriptionList)
            {
                long position = writer.BaseStream.Position;
                writer.Write(record.ID);
                position += IdObjetlen;
                if (record.Texte.Length >= DescriptionLen)
                {
                    record.Texte.Substring(0, DescriptionLen - 1);
                }
                writer.Write(record.Texte.ToCharArray());
                writer.Seek(DescriptionLen - record.Texte.Length, SeekOrigin.Current);
            }
            writer.Close();
            return true;
        }

    }
}
