using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using DesignData.SDS2;
using DesignData.SDS2.Database;
using DesignData.SDS2.Model;
using DesignData.SDS2.Detail;
using DesignData.SDS2.Primitives;
using DesignData.SDS2.Python;
using System.IO;
using System.Xml;



namespace Agen_SDS2_Export_Module_v0._2
{
    public partial class Form1 : Form
    {
        private static bool wasLinked = Linker.Link(MajorVersion.TwentyTwentyTwo);
        StringList jobs = new StringList();

        public Form1()
        {
            InitializeComponent();
        }

        public static string path = "";
        string assemblyLength = "";

        //string memberCOG = "";
        private void Form1_Load(object sender, EventArgs e)
        {
            btn_export.Enabled = false;
            btn_selected.Enabled = false;
            btn_populate.Enabled = false;
            bool isAgenFolderExist = Directory.Exists(@"C:\Agenexport");

            if (isAgenFolderExist)
            {
                path = @"C:\Agenexport";
            }
            else
            {
                Directory.CreateDirectory(@"C:\Agenexport");
                path = @"C:\Agenexport";
            }

            listView1.Columns.Add("ID", 50);
            listView1.Columns.Add("Assembly Name", 120);
            listView1.Columns.Add("Main Part Name", 120);
            listView1.Columns.Add("Phase Name", 80);
            listView1.Columns.Add("Lot", 50);
            listView1.Columns.Add("Page", 50);
            listView1.Columns.Add("Length", 80);
            listView1.Columns.Add("Width", 80);
            listView1.Columns.Add("Height", 80);
            listView1.Columns.Add("Weight", 80);

            DataDirectory.Open(DataDirectory.Default);
            RepositoryList repos = Repository.GetAllRepositories();

            foreach (Repository repo in repos)
            {
                foreach (Identifier id in repo.JobIdentifiers)
                {
                    jobs.Add(id.Name);
                }
            }
            foreach (string i in jobs)
            {
                comboBox1.Items.Add(i);
            }
        }

        private void btn_populate_Click(object sender, EventArgs e)
        {

            btn_selected.Enabled = true;
            btn_export.Enabled = true;
            if (wasLinked)
            {
                Job job = null;

                DataDirectory.Open(DataDirectory.Default);
                RepositoryList repos = Repository.GetAllRepositories();

                foreach (Repository repo in repos)
                {
                    foreach (Identifier id in repo.JobIdentifiers)
                    {
                        jobs.Add(id.Name);
                        if (id.Name.Contains(comboBox1.Text))
                        {
                            job = Job.FindJob(id);
                            job.Open();

                            bool isDirectoryExists = Directory.Exists($@"{path}" + "\\" + $"{id.Name}");

                            StreamReader streamReader = new StreamReader(@"C:\Users\Gokme\Desktop\config\ayarlar.txt");
                            string deneme = streamReader.ReadLine();
                            streamReader.Close();

                            if (path != deneme)
                            {
                                path = deneme;
                            }

                            if (isDirectoryExists)
                            {
                                path = $@"{path}" + "\\" + $"{id.Name}";
                            }
                            else
                            {
                                Directory.CreateDirectory($@"{path}" + "\\" + $"{id.Name}");
                                path = $@"{path}" + "\\" + $"{id.Name}";
                            }
                        }
                    }
                }

                MemberHandleList memberHandles = job.Members;

                foreach (MemberHandle mh in memberHandles)
                {
                    Member member = Member.Get(mh);
                    MaterialList ml = member.GetMaterial();

                    string id, assemblyName, partName, phaseName, lot, page, length, width, height, weight;

                    id = member.Number.ToString();
                    assemblyName = member.Piecemark;
                    partName = "";
                    phaseName = "";
                    lot = "";
                    page = "";
                    length = Convert.ToDouble(((member.Ends[1].Location - member.Ends[0].Location).Length * 25.4)).ToString();
                    assemblyLength = length;
                    width = "";
                    height = "";
                    weight = "";

                    foreach (Material mat in ml)
                    {
                        if (mat.IsMain)
                        {
                            partName = mat.Piecemark.ToString();
                        }
                        else
                        {
                            partName = "";
                        }

                        switch (mat)
                        {
                            case RectangularPlate rp:
                                width = Convert.ToDouble(rp.Width * 25.4).ToString();
                                break;
                            case WideFlange fl:

                                if (fl.IsMain)
                                {
                                    height = Convert.ToDouble(fl.OrderLength * 25.4).ToString();
                                    weight = Convert.ToDouble(fl.Weight * 0.45359237).ToString("0.00");
                                }
                                break;
                        }
                    }

                    ListViewItem listViewItem = new ListViewItem(id.ToString());
                    listViewItem.SubItems.Add(assemblyName);
                    listViewItem.SubItems.Add(partName);
                    listViewItem.SubItems.Add(phaseName);
                    listViewItem.SubItems.Add(lot);
                    listViewItem.SubItems.Add(page);
                    listViewItem.SubItems.Add(length);
                    listViewItem.SubItems.Add(width);
                    listViewItem.SubItems.Add(height);
                    listViewItem.SubItems.Add(weight);
                    listViewItem.Tag = (object)member;
                    this.listView1.Items.Add(listViewItem);
                }
            }
        }
        private void btn_export_Click(object sender, EventArgs e)
        {
            string jobName = "";
            if (wasLinked)
            {
                Job job = null;

                DataDirectory.Open(DataDirectory.Default);
                RepositoryList repos = Repository.GetAllRepositories();

                foreach (Repository repo in repos)
                {
                    foreach (Identifier id in repo.JobIdentifiers)
                    {
                        jobs.Add(id.Name);
                        if (id.Name.Contains(comboBox1.Text))
                        {
                            job = Job.FindJob(id);
                            job.Open();
                            jobName = id.Name;
                        }
                    }
                }

                MemberHandleList memberHandles = job.Members;


                foreach (MemberHandle mh in memberHandles)
                {
                    Member member = Member.Get(mh);

                    MaterialList ml = member.GetMaterial();


                    CNC_Configuration ncwriter = CNC_Configuration.Find("DSTV - AGEN");

                    StringList stl_Mat = new StringList();

                    string xmlFile = $"{member.Piecemark}.xml";

                    double assemblyWeight = 0;

                    // nc dosyasının oluşturulması
                    foreach (Material mat in ml)
                    {
                        string folderName = path;
                        string memberId = member.Piecemark.ToString();
                        string memberNumber = member.Number.ToString();

                        Directory.CreateDirectory($@"{folderName}" + "\\" + $"{memberId}" + "-" + $"{memberNumber}");
                        string pathString = $@"{folderName}" + "\\" + $"{memberId}" + "-" + $"{memberNumber}";

                        stl_Mat.Add(mat.Piecemark);
                        ncwriter.DownloadMaterials(stl_Mat, pathString);

                        assemblyWeight += mat.Weight;

                    }

                    //XML KISMI:

                    XmlTextWriter xmlWriter = new XmlTextWriter($@"{path}\{member.Piecemark}-{member.Number}\{xmlFile}", UTF8Encoding.UTF8);
                    double wlength = 0;

                    xmlWriter.Formatting = Formatting.Indented;

                    xmlWriter.WriteStartDocument();

                    //CASDS2AssemblyData
                    xmlWriter.WriteStartElement("CASDS2AssemblyData");

                    xmlWriter.WriteAttributeString("xmlns:xsi", "http://www.abkaotomasyon.com/CASDS2AssemblyData.xsd");
                    xmlWriter.WriteAttributeString("xsi:noNameSpaceSchemaLocation", "CASDS2AssemblyData.xsd");


                    xmlWriter.WriteElementString("copyright", "Copyright Abka Automation 2022. All rights reserved.");
                    xmlWriter.WriteElementString("Source", "SDS2");
                    xmlWriter.WriteElementString("MeasuringSystem", "1");
                    xmlWriter.WriteElementString("File_Created_Date", $"{DateTime.Now.ToShortDateString()}");

                    xmlWriter.WriteStartElement("Model");
                    xmlWriter.WriteElementString("ModelName", $"{jobName}");

                    xmlWriter.WriteStartElement("Assemblies");

                    xmlWriter.WriteStartElement("Assembly");
                    xmlWriter.WriteElementString("AssemblyPos", $"{member.Piecemark}");
                    xmlWriter.WriteElementString("AssemblyName", $"{member.Piecemark}");
                    xmlWriter.WriteElementString("AssemblyID", $"{member.Number}");
                    xmlWriter.WriteElementString("AssemblyWeight", $"{(assemblyWeight * 0.45359237).ToString("0.00")}");

                    //COG
                    xmlWriter.WriteStartElement("COG");

                    xmlWriter.WriteAttributeString("X", "0");
                    xmlWriter.WriteAttributeString("Y", "0");
                    xmlWriter.WriteAttributeString("Z", "0");

                    //COG
                    xmlWriter.WriteEndElement();

                    //xmlWriter.WriteElementString("COG: ", "X: Y: Z:");



                    xmlWriter.WriteElementString("Phase", "Phase 1");
                    xmlWriter.WriteElementString("Lot", "\n\t");

                    xmlWriter.WriteStartElement("Parts");

                    int partID = 0;

                    foreach (Material mat in ml)
                    {
                        xmlWriter.WriteStartElement("Part");
                        xmlWriter.WriteElementString("PartMark", $"{mat.Piecemark}");

                        bool _iscolumn = false;
                        if (mat.ToGlobalCoordinates.XAxis.Z == 1) { _iscolumn = true; }
                        

                        switch (mat)
                        {
                            case Plate rp:
                                xmlWriter.WriteElementString("PartName", "PLATE");
                                break;
                            case RolledShapeMaterial fl:
                                if (_iscolumn) { xmlWriter.WriteElementString("PartName", "COLUMN"); }
                                else { xmlWriter.WriteElementString("PartName", "BEAM"); }//kiriş kontolü yapılacak
                                break;

                        }
                        xmlWriter.WriteElementString("PartPos", $"{mat.Piecemark}");
                        xmlWriter.WriteElementString("PartID", $"{partID}");
                        xmlWriter.WriteElementString("MainMember", $"{mat.IsMain.ToString().ToUpper()}");

                        switch (mat)
                        {
                            case Plate rp:
                                xmlWriter.WriteElementString("PartProfileType", "B");
                                break;
                            case WideFlange fl:
                                xmlWriter.WriteElementString("PartProfileType", "I");
                                break;
                            case Angle a:
                                xmlWriter.WriteElementString("PartProfileType", "L");
                                break;
                            case Channel chan:
                                xmlWriter.WriteElementString("PartProfileType", "U");
                                break;
                        }
                        xmlWriter.WriteElementString("PartProfile", $"{mat.Description}");
                        switch (mat)
                        {
                            case RolledShapeMaterial fl:
                                if (_iscolumn)
                                { xmlWriter.WriteElementString("IsColumn", "TRUE"); }
                                else
                                {
                                    xmlWriter.WriteElementString("IsColumn", "FALSE");
                                }

                                 //xmlWriter.WriteElementString("IsColumn", "TRUE");
                                    break;
                            default:
                                xmlWriter.WriteElementString("IsColumn", "FALSE");
                                break;
                        }
                        xmlWriter.WriteElementString("PartMaterial", $"{mat.Grade.Name}");
                        switch (mat)
                        {
                            case RectangularPlate rp:
                                xmlWriter.WriteElementString("PartLength", $"{(rp.OrderLength * 25.4).ToString("0.00")}");
                                xmlWriter.WriteElementString("PartWeight", $"{(rp.Weight * 0.45359237).ToString("0.00")}");
                                xmlWriter.WriteElementString("PartWidth", $"{rp.Width * 25.4}");
                                xmlWriter.WriteElementString("PartHeight", "0.00");
                                xmlWriter.WriteElementString("PartFlangeThickness", "0.00");
                                xmlWriter.WriteElementString("PartWebThickness", $"{(rp.Thickness * 25.4).ToString("0.00")}");
                                break;
                            case WideFlange fl:
                                xmlWriter.WriteElementString("PartLength", $"{fl.OrderLength * 25.4}");
                                wlength = fl.OrderLength * 25.4;
                                xmlWriter.WriteElementString("PartWeight", $"{(fl.Weight * 0.45359237).ToString("0.00")}");
                                xmlWriter.WriteElementString("PartWidth", "0.00");
                                xmlWriter.WriteElementString("PartHeight", "0.00");
                                xmlWriter.WriteElementString("PartFlangeThickness", "0.00");
                                xmlWriter.WriteElementString("PartWeBThickness", "0.00");
                                break;
                            case Angle a:
                                xmlWriter.WriteElementString("PartLength", $"{a.OrderLength * 25.4}");
                                wlength = a.OrderLength * 25.4;
                                xmlWriter.WriteElementString("PartWeight", $"{(a.Weight * 0.45359237).ToString("0.00")}");
                                xmlWriter.WriteElementString("PartWidth", "0.00");
                                xmlWriter.WriteElementString("PartHeight", "0.00");
                                xmlWriter.WriteElementString("PartFlangeThickness", "0.00");
                                xmlWriter.WriteElementString("PartWeBThickness", "0.00");
                                break;
                            case Channel chan:
                                xmlWriter.WriteElementString("PartLength", $"{chan.OrderLength * 25.4}");
                                wlength = chan.OrderLength * 25.4;
                                xmlWriter.WriteElementString("PartWeight", $"{(chan.Weight * 0.45359237).ToString("0.00")}");
                                xmlWriter.WriteElementString("PartWidth", "0.00");
                                xmlWriter.WriteElementString("PartHeight", "0.00");
                                xmlWriter.WriteElementString("PartFlangeThickness", "0.00");
                                xmlWriter.WriteElementString("PartWeBThickness", "0.00");
                                break;
                        }
                        xmlWriter.WriteElementString("PartRotationAngle", "0.00");

                        xmlWriter.WriteStartElement("PartStartPoint");
                        xmlWriter.WriteAttributeString("X", $"{((mat.ToGlobalCoordinates.Origin.X) * 25.4).ToString("0.00")}");
                        xmlWriter.WriteAttributeString("Y", $"{((mat.ToGlobalCoordinates.Origin.Y) * 25.4).ToString("0.00")}");
                        xmlWriter.WriteAttributeString("Z", $"{((mat.ToGlobalCoordinates.Origin.Z) * 25.4).ToString("0.00")}");
                        xmlWriter.WriteEndElement();

                        xmlWriter.WriteStartElement("PartEndPoint");
                        xmlWriter.WriteAttributeString("X", $"{((member.Ends[1].Location.X) * 25.4).ToString("0.00")}");
                        xmlWriter.WriteAttributeString("Y", $"{((member.Ends[1].Location.Y) * 25.4).ToString("0.00")}");
                        xmlWriter.WriteAttributeString("Z", $"{((member.Ends[1].Location.Z) * 25.4).ToString("0.00")}");
                        xmlWriter.WriteEndElement();

                        xmlWriter.WriteStartElement("DSTVCS");
                        //xmlWriter.WriteAttributeString("X", $"{(mat.ToGlobalCoordinates.XAxis.X + "," + mat.ToGlobalCoordinates.XAxis.Y + "," + mat.ToGlobalCoordinates.XAxis.Z)}");
                        
                        xmlWriter.WriteStartElement("XAxis");
                        xmlWriter.WriteAttributeString("X", $"{(mat.ToGlobalCoordinates.XAxis.X)}");
                        xmlWriter.WriteAttributeString("Y", $"{(mat.ToGlobalCoordinates.XAxis.Y)}");
                        xmlWriter.WriteAttributeString("Z", $"{(mat.ToGlobalCoordinates.XAxis.Z)}");
                        xmlWriter.WriteEndElement();

                        xmlWriter.WriteStartElement("YAxis");
                        xmlWriter.WriteAttributeString("X", $"{(mat.ToGlobalCoordinates.YAxis.X)}");
                        xmlWriter.WriteAttributeString("Y", $"{(mat.ToGlobalCoordinates.YAxis.Y)}");
                        xmlWriter.WriteAttributeString("Z", $"{(mat.ToGlobalCoordinates.YAxis.Z)}");
                        xmlWriter.WriteEndElement();

                        xmlWriter.WriteStartElement("ZAxis");
                        xmlWriter.WriteAttributeString("X", $"{(mat.ToGlobalCoordinates.ZAxis.X)}");
                        xmlWriter.WriteAttributeString("Y", $"{(mat.ToGlobalCoordinates.ZAxis.Y)}");
                        xmlWriter.WriteAttributeString("Z", $"{(mat.ToGlobalCoordinates.ZAxis.Z)}");
                        xmlWriter.WriteEndElement();

                        xmlWriter.WriteEndElement();

                        //xmlWriter.WriteStartElement("RotationMatrix");
                        //xmlWriter.WriteAttributeString("X", $"{(mat.ToGlobalCoordinates.XAxis.X+","+ mat.ToGlobalCoordinates.XAxis.Y+ "," + mat.ToGlobalCoordinates.XAxis.Z)}");
                        //xmlWriter.WriteAttributeString("Y", $"{(mat.ToGlobalCoordinates.YAxis)}");
                        //xmlWriter.WriteAttributeString("Z", $"{(mat.ToGlobalCoordinates.ZAxis)}");
                        //xmlWriter.WriteEndElement();

                        switch (mat)
                        {
                            case WideFlange wf:
                                xmlWriter.WriteElementString("MaterialOffset", (Convert.ToDouble(assemblyLength) - wlength).ToString("0.00"));
                                break;
                            case RectangularPlate rp:
                                xmlWriter.WriteElementString("MaterialOffset", $"{rp.ToGlobalCoordinates}");
                                break;
                        }
                        
                        xmlWriter.WriteElementString("IsContourPlate", "False");
                        xmlWriter.WriteElementString("PartHoles", "\n\t    ");

                        //part
                        xmlWriter.WriteEndElement();

                        partID++;

                    }

                    //parts
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("Welds");

                    WeldList wl = member.GetWelds();

                    foreach (Weld w in wl)
                    {
                        

                        xmlWriter.WriteStartElement("Weld");
                        xmlWriter.WriteAttributeString("HasPhysicalOtherWeld", "0");

                        if (w.OtherStitchType.ToString() == "NoStitch")
                        {
                            xmlWriter.WriteElementString("StitchType", "None");
                        }
                        else
                        {
                            xmlWriter.WriteElementString("StitchType", $"{w.OtherStitchType}");
                        }
                        
                        xmlWriter.WriteElementString("GUID", $"{w.Guid}");
                        xmlWriter.WriteStartElement("ArrowWeld");

                        xmlWriter.WriteStartElement("WeldedParts");
                        
                        xmlWriter.WriteStartElement("WeldedPart");
                        xmlWriter.WriteAttributeString("AssemblyID", $"{member.Number}");
                        xmlWriter.WriteAttributeString("PartID", "0");
                        xmlWriter.WriteAttributeString("Surface", "Unknown");
                        xmlWriter.WriteEndElement();

                        xmlWriter.WriteStartElement("WeldedPart");
                        xmlWriter.WriteAttributeString("AssemblyID", $"{member.Number}");
                        xmlWriter.WriteAttributeString("PartID", "1");
                        xmlWriter.WriteAttributeString("Surface", "Unknown");
                        xmlWriter.WriteEndElement();

                        //weldedparts
                        xmlWriter.WriteEndElement();

                        xmlWriter.WriteStartElement("WeldPathModel");
                        
                        xmlWriter.WriteStartElement("WeldPoint");
                        xmlWriter.WriteAttributeString("X", $"{w.ToGlobalCoordinates.XAxis.Length * 25.4}");
                        xmlWriter.WriteAttributeString("Y", $"{w.ToGlobalCoordinates.YAxis.Length * 25.4}");
                        xmlWriter.WriteAttributeString("Z", $"{w.ToGlobalCoordinates.ZAxis.Length * 25.4}");
                        xmlWriter.WriteEndElement();

                        xmlWriter.WriteStartElement("WeldPoint");
                        xmlWriter.WriteAttributeString("X", $"{w.ToGlobalCoordinates.XAxis.Length * 25.4}");
                        xmlWriter.WriteAttributeString("Y", $"{w.ToGlobalCoordinates.YAxis.Length * 25.4}");
                        xmlWriter.WriteAttributeString("Z", $"{w.ToGlobalCoordinates.ZAxis.Length * 25.4}");
                        xmlWriter.WriteEndElement();
                        
                        //weldpathmodel
                        xmlWriter.WriteEndElement();

                        xmlWriter.WriteStartElement("WeldPathAssembly");

                        xmlWriter.WriteStartElement("WeldPoint");
                        xmlWriter.WriteAttributeString("X", $"{w.ToMemberCoordinates.XAxis.Length * 25.4}");
                        xmlWriter.WriteAttributeString("Y", $"{w.ToMemberCoordinates.YAxis.Length * 25.4}");
                        xmlWriter.WriteAttributeString("Z", $"{w.ToMemberCoordinates.ZAxis.Length * 25.4}");
                        xmlWriter.WriteEndElement();

                        xmlWriter.WriteStartElement("WeldPoint");
                        xmlWriter.WriteAttributeString("X", $"{w.ToMemberCoordinates.XAxis.Length * 25.4}");
                        xmlWriter.WriteAttributeString("Y", $"{w.ToMemberCoordinates.YAxis.Length * 25.4}");
                        xmlWriter.WriteAttributeString("Z", $"{w.ToMemberCoordinates.ZAxis.Length * 25.4}");
                        xmlWriter.WriteEndElement();

                        //weldpathassembly
                        xmlWriter.WriteEndElement();

                        xmlWriter.WriteElementString("WeldType", $"{w.ArrowSide.WeldType}");
                        xmlWriter.WriteElementString("WeldSize", $"{(w.ArrowSide.WeldSize * 25.4).ToString("0.00")}");
                        xmlWriter.WriteElementString("WeldLength", $"{(w.ArrowSide.WeldLength * 25.4).ToString("0.00")}");
                        xmlWriter.WriteElementString("GrooveAngle", $"{(w.ArrowSide.GrooveAngle).ToString("0.00")}");
                        xmlWriter.WriteElementString("RootOpening", $"{(w.ArrowSide.RootOpening).ToString("0.00")}");
                        xmlWriter.WriteElementString("RootFace", $"{(w.ArrowSide.RootFace).ToString("0.00")}");
                        xmlWriter.WriteElementString("StaggerLength", $"{(w.ArrowSide.StitchLength).ToString("0.00")}");
                        xmlWriter.WriteElementString("StaggerSpacing", $"{(w.ArrowSide.StitchSpacing).ToString("0.00")}");
                        xmlWriter.WriteElementString("StaggerTermination1", $"{(w.ArrowSide.StitchLeftTermination).ToString("0.00")}");
                        xmlWriter.WriteElementString("StaggerTermination2", $"{(w.ArrowSide.StitchRightTermination).ToString("0.00")}");
                        xmlWriter.WriteElementString("HoldBack1", $"{(w.ArrowSide.LeftSetback).ToString("0.00")}");
                        xmlWriter.WriteElementString("HoldBack2", $"{(w.ArrowSide.RightSetback).ToString("0.00")}");
                        xmlWriter.WriteElementString("Shape", "None");

                        //arrowweld
                        xmlWriter.WriteEndElement();

                        xmlWriter.WriteStartElement("OtherWeld");
                        xmlWriter.WriteElementString("WeldType", null);
                        xmlWriter.WriteElementString("WeldType", $"{w.OtherSide.WeldType}");
                        xmlWriter.WriteElementString("WeldSize", $"{(w.OtherSide.WeldSize * 25.4).ToString("0.00")}");
                        xmlWriter.WriteElementString("WeldLength", $"{(w.OtherSide.WeldLength * 25.4).ToString("0.00")}");
                        xmlWriter.WriteElementString("GrooveAngle", $"{(w.OtherSide.GrooveAngle).ToString("0.00")}");
                        xmlWriter.WriteElementString("RootOpening", $"{(w.OtherSide.RootOpening).ToString("0.00")}");
                        xmlWriter.WriteElementString("RootFace", $"{(w.OtherSide.RootFace).ToString("0.00")}");
                        xmlWriter.WriteElementString("StaggerLength", $"{(w.OtherSide.StitchLength).ToString("0.00")}");
                        xmlWriter.WriteElementString("StaggerSpacing", $"{(w.OtherSide.StitchSpacing).ToString("0.00")}");
                        xmlWriter.WriteElementString("StaggerTermination1", $"{(w.OtherSide.StitchLeftTermination).ToString("0.00")}");
                        xmlWriter.WriteElementString("StaggerTermination2", $"{(w.OtherSide.StitchRightTermination).ToString("0.00")}");
                        xmlWriter.WriteElementString("HoldBack1", $"{(w.OtherSide.LeftSetback).ToString("0.00")}");
                        xmlWriter.WriteElementString("HoldBack2", $"{(w.OtherSide.RightSetback).ToString("0.00")}");
                        xmlWriter.WriteElementString("Shape", "None");

                        //otherweld
                        xmlWriter.WriteEndElement();

                        xmlWriter.WriteElementString("FieldWeld", $"{w.IsFieldWeld}");
                        xmlWriter.WriteElementString("PreQualified", $"{w.IsSystemGenerated}");
                        xmlWriter.WriteElementString("JointDesignation", $"{w.JointDesignation}");
                        xmlWriter.WriteElementString("JointType", $"{w.WeldJoint}");
                        xmlWriter.WriteElementString("PenType", $"{w.Penetration}");
                        xmlWriter.WriteElementString("ProcessType", $"{w.Process}");
                        xmlWriter.WriteElementString("Position", $"{w.Position}");

                        //weld
                        xmlWriter.WriteEndElement();

                    }

                    //welds
                    xmlWriter.WriteEndElement();




                    //assembly
                    xmlWriter.WriteEndElement();
                    //assemblies
                    xmlWriter.WriteEndElement();
                    //model
                    xmlWriter.WriteEndElement();
                    //assemblydata
                    xmlWriter.WriteEndElement();
                    xmlWriter.WriteEndDocument();
                    xmlWriter.Close();

                    //XML KISMI SONU

                }
            }
        }

        private void btn_clear_Click(object sender, EventArgs e)
        {
            listView1.Items.Clear();
        }

        private void btn_remove_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem eachItem in listView1.SelectedItems)
            {
                listView1.Items.Remove(eachItem);
            }
        }
        private void btn_selected_Click(object sender, EventArgs e)
        {
            string jobName = "";
            if (wasLinked)
            {
                Job job = null;

                DataDirectory.Open(DataDirectory.Default);
                RepositoryList repos = Repository.GetAllRepositories();

                foreach (Repository repo in repos)
                {
                    foreach (Identifier id in repo.JobIdentifiers)
                    {
                        jobs.Add(id.Name);
                        if (id.Name.Contains(comboBox1.Text))
                        {
                            job = Job.FindJob(id);
                            job.Open();
                            jobName = id.Name;
                        }
                    }
                }

                MemberHandleList memberHandles = job.Members;

                foreach (MemberHandle mh in memberHandles)
                {
                    Member member = Member.Get(mh);

                    MaterialList ml = member.GetMaterial();

                    CNC_Configuration ncwriter = CNC_Configuration.Find("DSTV - AGEN");

                    StringList stl_Mat = new StringList();

                    StringList deneme = new StringList();

                    string xmlFile = $"{member.Piecemark}.xml";

                    foreach (ListViewItem eachItem in listView1.SelectedItems)
                    {
                        if (eachItem.SubItems[0].Text == member.Number.ToString())
                        {
                            double assemblyWeight = 0;
                            foreach (Material mat in ml)
                            {
                                string folderName = path;
                                string memberId = member.Piecemark.ToString();
                                string memberNumber = member.Number.ToString();

                                Directory.CreateDirectory($@"{folderName}" + "\\" + $"{memberId}" + "-" + $"{memberNumber}");
                                string pathString = $@"{folderName}" + "\\" + $"{memberId}" + "-" + $"{memberNumber}";

                                deneme.Add(mat.Piecemark);
                                ncwriter.DownloadMaterials(deneme, pathString);
                                assemblyWeight += mat.Weight;
                            }
                            //XML KISMI:

                            XmlTextWriter xmlWriter = new XmlTextWriter($@"{path}\{member.Piecemark}-{member.Number}\{xmlFile}", UTF8Encoding.UTF8);
                            double wlength = 0;

                            xmlWriter.Formatting = Formatting.Indented;

                            xmlWriter.WriteStartDocument();

                            //CASDS2AssemblyData
                            xmlWriter.WriteStartElement("CASDS2AssemblyData");

                            xmlWriter.WriteAttributeString("xmlns:xsi", "http://www.abkaotomasyon.com/CASDS2AssemblyData.xsd");
                            xmlWriter.WriteAttributeString("xsi:noNameSpaceSchemaLocation", "CASDS2AssemblyData.xsd");


                            xmlWriter.WriteElementString("copyright", "Copyright Abka Automation 2022. All rights reserved.");
                            xmlWriter.WriteElementString("Source", "SDS2");
                            xmlWriter.WriteElementString("MeasuringSystem", "1");
                            xmlWriter.WriteElementString("File_Created_Date", $"{DateTime.Now.ToShortDateString()}");

                            xmlWriter.WriteStartElement("Model");
                            xmlWriter.WriteElementString("ModelName", $"{jobName}");

                            xmlWriter.WriteStartElement("Assemblies");

                            xmlWriter.WriteStartElement("Assembly");
                            xmlWriter.WriteElementString("AssemblyPos", $"{member.Piecemark}");
                            xmlWriter.WriteElementString("AssemblyName", $"{member.Piecemark}");
                            xmlWriter.WriteElementString("AssemblyID", $"{member.Number}");
                            xmlWriter.WriteElementString("AssemblyWeight", $"{(assemblyWeight * 0.45359237).ToString("0.00")}");

                            //COG
                            xmlWriter.WriteStartElement("COG");

                            xmlWriter.WriteAttributeString("X", "0");
                            xmlWriter.WriteAttributeString("Y", "0");
                            xmlWriter.WriteAttributeString("Z", "0");

                            //COG
                            xmlWriter.WriteEndElement();

                            //xmlWriter.WriteElementString("COG: ", "X: Y: Z:");


                            xmlWriter.WriteElementString("Phase", "Phase 1");
                            xmlWriter.WriteElementString("Lot", "\n\t");

                            xmlWriter.WriteStartElement("Parts");

                            int partID = 0;

                            foreach (Material mat in ml)
                            {
                                xmlWriter.WriteStartElement("Part");
                                xmlWriter.WriteElementString("PartMark", $"{mat.Piecemark}");
                                switch (mat)
                                {
                                    case Plate rp:
                                        xmlWriter.WriteElementString("PartName", "PLATE");
                                        break;
                                    case RolledShapeMaterial fl:
                                        xmlWriter.WriteElementString("PartName", "COLUMN");
                                        break;
                                }
                                xmlWriter.WriteElementString("PartPos", $"{mat.Piecemark}");
                                xmlWriter.WriteElementString("PartID", $"{partID}");
                                xmlWriter.WriteElementString("MainMember", $"{mat.IsMain.ToString().ToUpper()}");

                                switch (mat)
                                {
                                    case Plate rp:
                                        xmlWriter.WriteElementString("PartProfileType", "B");
                                        break;
                                    case WideFlange fl:
                                        xmlWriter.WriteElementString("PartProfileType", "I");
                                        break;
                                    case Angle a:
                                        xmlWriter.WriteElementString("PartProfileType", "L");
                                        break;
                                    case Channel chan:
                                        xmlWriter.WriteElementString("PartProfileType", "U");
                                        break;
                                }
                                xmlWriter.WriteElementString("PartProfile", $"{mat.Description}");
                                switch (mat)
                                {
                                    case RolledShapeMaterial fl:
                                        xmlWriter.WriteElementString("IsColumn", "TRUE");
                                        break;
                                    default:
                                        xmlWriter.WriteElementString("IsColumn", "FALSE");
                                        break;
                                }
                                xmlWriter.WriteElementString("PartMaterial", $"{mat.Grade.Name}");
                                switch (mat)
                                {
                                    case RectangularPlate rp:
                                        xmlWriter.WriteElementString("PartLength", $"{(rp.OrderLength * 25.4).ToString("0.00")}");
                                        xmlWriter.WriteElementString("PartWeight", $"{(rp.Weight * 0.45359237).ToString("0.00")}");
                                        xmlWriter.WriteElementString("PartWidth", $"{rp.Width * 25.4}");
                                        xmlWriter.WriteElementString("PartHeight", "0.00");
                                        xmlWriter.WriteElementString("PartFlangeThickness", "0.00");
                                        xmlWriter.WriteElementString("PartWebThickness", $"{(rp.Thickness * 25.4).ToString("0.00")}");
                                        break;
                                    case WideFlange fl:
                                        xmlWriter.WriteElementString("PartLength", $"{fl.OrderLength * 25.4}");
                                        wlength = fl.OrderLength * 25.4;
                                        xmlWriter.WriteElementString("PartWeight", $"{(fl.Weight * 0.45359237).ToString("0.00")}");
                                        xmlWriter.WriteElementString("PartWidth", "0.00");
                                        xmlWriter.WriteElementString("PartHeight", "0.00");
                                        xmlWriter.WriteElementString("PartFlangeThickness", "0.00");
                                        xmlWriter.WriteElementString("PartWeBThickness", "0.00");
                                        break;
                                    case Angle a:
                                        xmlWriter.WriteElementString("PartLength", $"{a.OrderLength * 25.4}");
                                        wlength = a.OrderLength * 25.4;
                                        xmlWriter.WriteElementString("PartWeight", $"{(a.Weight * 0.45359237).ToString("0.00")}");
                                        xmlWriter.WriteElementString("PartWidth", "0.00");
                                        xmlWriter.WriteElementString("PartHeight", "0.00");
                                        xmlWriter.WriteElementString("PartFlangeThickness", "0.00");
                                        xmlWriter.WriteElementString("PartWeBThickness", "0.00");
                                        break;
                                    case Channel chan:
                                        xmlWriter.WriteElementString("PartLength", $"{chan.OrderLength * 25.4}");
                                        wlength = chan.OrderLength * 25.4;
                                        xmlWriter.WriteElementString("PartWeight", $"{(chan.Weight * 0.45359237).ToString("0.00")}");
                                        xmlWriter.WriteElementString("PartWidth", "0.00");
                                        xmlWriter.WriteElementString("PartHeight", "0.00");
                                        xmlWriter.WriteElementString("PartFlangeThickness", "0.00");
                                        xmlWriter.WriteElementString("PartWeBThickness", "0.00");
                                        break;
                                }
                                xmlWriter.WriteElementString("PartRotationAngle", "0.00");

                                xmlWriter.WriteStartElement("PartStartPoint");
                                xmlWriter.WriteAttributeString("X", $"{((mat.ToGlobalCoordinates.Origin.X) * 25.4).ToString("0.00")}");
                                xmlWriter.WriteAttributeString("Y", $"{((mat.ToGlobalCoordinates.Origin.Y) * 25.4).ToString("0.00")}");
                                xmlWriter.WriteAttributeString("Z", $"{((mat.ToGlobalCoordinates.Origin.Z) * 25.4).ToString("0.00")}");
                                xmlWriter.WriteEndElement();

                                xmlWriter.WriteStartElement("PartEndPoint");
                                xmlWriter.WriteAttributeString("X", $"{((member.Ends[1].Location.X) * 25.4).ToString("0.00")}");
                                xmlWriter.WriteAttributeString("Y", $"{((member.Ends[1].Location.Y) * 25.4).ToString("0.00")}");
                                xmlWriter.WriteAttributeString("Z", $"{((member.Ends[1].Location.Z) * 25.4).ToString("0.00")}");
                                xmlWriter.WriteEndElement();

                                switch (mat)
                                {
                                    case WideFlange wf:
                                        xmlWriter.WriteElementString("MaterialOffset", (Convert.ToDouble(assemblyLength) - wlength).ToString("0.00"));
                                        break;
                                    case RectangularPlate rp:
                                        xmlWriter.WriteElementString("MaterialOffset", $"{rp.ToGlobalCoordinates}");
                                        break;
                                }

                                xmlWriter.WriteElementString("IsContourPlate", "False");
                                xmlWriter.WriteElementString("PartHoles", "\n\t    ");

                                //part
                                xmlWriter.WriteEndElement();

                                partID++;

                            }

                            //parts
                            xmlWriter.WriteEndElement();

                            xmlWriter.WriteStartElement("Welds");

                            WeldList wl = member.GetWelds();

                            foreach (Weld w in wl)
                            {


                                xmlWriter.WriteStartElement("Weld");
                                xmlWriter.WriteAttributeString("HasPhysicalOtherWeld", "0");

                                if (w.OtherStitchType.ToString() == "NoStitch")
                                {
                                    xmlWriter.WriteElementString("StitchType", "None");
                                }
                                else
                                {
                                    xmlWriter.WriteElementString("StitchType", $"{w.OtherStitchType}");
                                }

                                xmlWriter.WriteElementString("GUID", $"{w.Guid}");
                                xmlWriter.WriteStartElement("ArrowWeld");

                                xmlWriter.WriteStartElement("WeldedParts");

                                xmlWriter.WriteStartElement("WeldedPart");
                                xmlWriter.WriteAttributeString("AssemblyID", $"{member.Number}");
                                xmlWriter.WriteAttributeString("PartID", "0");
                                xmlWriter.WriteAttributeString("Surface", "Unknown");
                                xmlWriter.WriteEndElement();

                                xmlWriter.WriteStartElement("WeldedPart");
                                xmlWriter.WriteAttributeString("AssemblyID", $"{member.Number}");
                                xmlWriter.WriteAttributeString("PartID", "1");
                                xmlWriter.WriteAttributeString("Surface", "Unknown");
                                xmlWriter.WriteEndElement();

                                //weldedparts
                                xmlWriter.WriteEndElement();

                                xmlWriter.WriteStartElement("WeldPathModel");

                                xmlWriter.WriteStartElement("WeldPoint");
                                xmlWriter.WriteAttributeString("X", $"{w.ToGlobalCoordinates.XAxis.Length * 25.4}");
                                xmlWriter.WriteAttributeString("Y", $"{w.ToGlobalCoordinates.YAxis.Length * 25.4}");
                                xmlWriter.WriteAttributeString("Z", $"{w.ToGlobalCoordinates.ZAxis.Length * 25.4}");
                                xmlWriter.WriteEndElement();

                                xmlWriter.WriteStartElement("WeldPoint");
                                xmlWriter.WriteAttributeString("X", $"{w.ToGlobalCoordinates.XAxis.Length * 25.4}");
                                xmlWriter.WriteAttributeString("Y", $"{w.ToGlobalCoordinates.YAxis.Length * 25.4}");
                                xmlWriter.WriteAttributeString("Z", $"{w.ToGlobalCoordinates.ZAxis.Length * 25.4}");
                                xmlWriter.WriteEndElement();

                                //weldpathmodel
                                xmlWriter.WriteEndElement();

                                xmlWriter.WriteStartElement("WeldPathAssembly");

                                xmlWriter.WriteStartElement("WeldPoint");
                                xmlWriter.WriteAttributeString("X", $"{w.ToMemberCoordinates.XAxis.Length * 25.4}");
                                xmlWriter.WriteAttributeString("Y", $"{w.ToMemberCoordinates.YAxis.Length * 25.4}");
                                xmlWriter.WriteAttributeString("Z", $"{w.ToMemberCoordinates.ZAxis.Length * 25.4}");
                                xmlWriter.WriteEndElement();

                                xmlWriter.WriteStartElement("WeldPoint");
                                xmlWriter.WriteAttributeString("X", $"{w.ToMemberCoordinates.XAxis.Length * 25.4}");
                                xmlWriter.WriteAttributeString("Y", $"{w.ToMemberCoordinates.YAxis.Length * 25.4}");
                                xmlWriter.WriteAttributeString("Z", $"{w.ToMemberCoordinates.ZAxis.Length * 25.4}");
                                xmlWriter.WriteEndElement();

                                //weldpathassembly
                                xmlWriter.WriteEndElement();

                                xmlWriter.WriteElementString("WeldType", $"{w.ArrowSide.WeldType}");
                                xmlWriter.WriteElementString("WeldSize", $"{(w.ArrowSide.WeldSize * 25.4).ToString("0.00")}");
                                xmlWriter.WriteElementString("WeldLength", $"{(w.ArrowSide.WeldLength * 25.4).ToString("0.00")}");
                                xmlWriter.WriteElementString("GrooveAngle", $"{(w.ArrowSide.GrooveAngle).ToString("0.00")}");
                                xmlWriter.WriteElementString("RootOpening", $"{(w.ArrowSide.RootOpening).ToString("0.00")}");
                                xmlWriter.WriteElementString("RootFace", $"{(w.ArrowSide.RootFace).ToString("0.00")}");
                                xmlWriter.WriteElementString("StaggerLength", $"{(w.ArrowSide.StitchLength).ToString("0.00")}");
                                xmlWriter.WriteElementString("StaggerSpacing", $"{(w.ArrowSide.StitchSpacing).ToString("0.00")}");
                                xmlWriter.WriteElementString("StaggerTermination1", $"{(w.ArrowSide.StitchLeftTermination).ToString("0.00")}");
                                xmlWriter.WriteElementString("StaggerTermination2", $"{(w.ArrowSide.StitchRightTermination).ToString("0.00")}");
                                xmlWriter.WriteElementString("HoldBack1", $"{(w.ArrowSide.LeftSetback).ToString("0.00")}");
                                xmlWriter.WriteElementString("HoldBack2", $"{(w.ArrowSide.RightSetback).ToString("0.00")}");
                                xmlWriter.WriteElementString("Shape", "None");

                                //arrowweld
                                xmlWriter.WriteEndElement();

                                xmlWriter.WriteStartElement("OtherWeld");
                                xmlWriter.WriteElementString("WeldType", null);
                                xmlWriter.WriteElementString("WeldType", $"{w.OtherSide.WeldType}");
                                xmlWriter.WriteElementString("WeldSize", $"{(w.OtherSide.WeldSize * 25.4).ToString("0.00")}");
                                xmlWriter.WriteElementString("WeldLength", $"{(w.OtherSide.WeldLength * 25.4).ToString("0.00")}");
                                xmlWriter.WriteElementString("GrooveAngle", $"{(w.OtherSide.GrooveAngle).ToString("0.00")}");
                                xmlWriter.WriteElementString("RootOpening", $"{(w.OtherSide.RootOpening).ToString("0.00")}");
                                xmlWriter.WriteElementString("RootFace", $"{(w.OtherSide.RootFace).ToString("0.00")}");
                                xmlWriter.WriteElementString("StaggerLength", $"{(w.OtherSide.StitchLength).ToString("0.00")}");
                                xmlWriter.WriteElementString("StaggerSpacing", $"{(w.OtherSide.StitchSpacing).ToString("0.00")}");
                                xmlWriter.WriteElementString("StaggerTermination1", $"{(w.OtherSide.StitchLeftTermination).ToString("0.00")}");
                                xmlWriter.WriteElementString("StaggerTermination2", $"{(w.OtherSide.StitchRightTermination).ToString("0.00")}");
                                xmlWriter.WriteElementString("HoldBack1", $"{(w.OtherSide.LeftSetback).ToString("0.00")}");
                                xmlWriter.WriteElementString("HoldBack2", $"{(w.OtherSide.RightSetback).ToString("0.00")}");
                                xmlWriter.WriteElementString("Shape", "None");

                                //otherweld
                                xmlWriter.WriteEndElement();

                                xmlWriter.WriteElementString("FieldWeld", $"{w.IsFieldWeld}");
                                xmlWriter.WriteElementString("PreQualified", $"{w.IsSystemGenerated}");
                                xmlWriter.WriteElementString("JointDesignation", $"{w.JointDesignation}");
                                xmlWriter.WriteElementString("JointType", $"{w.WeldJoint}");
                                xmlWriter.WriteElementString("PenType", $"{w.Penetration}");
                                xmlWriter.WriteElementString("ProcessType", $"{w.Process}");
                                xmlWriter.WriteElementString("Position", $"{w.Position}");

                                //weld
                                xmlWriter.WriteEndElement();

                            }

                            //welds
                            xmlWriter.WriteEndElement();




                            //assembly
                            xmlWriter.WriteEndElement();
                            //assemblies
                            xmlWriter.WriteEndElement();
                            //model
                            xmlWriter.WriteEndElement();
                            //assemblydata
                            xmlWriter.WriteEndElement();
                            xmlWriter.WriteEndDocument();
                            xmlWriter.Close();

                            //XML KISMI SONU
                        }
                    }
                }
            }
        }

        private void btn_search_Click(object sender, EventArgs e)
        {
            string text = txt_search.Text.ToUpper();

            foreach (ListViewItem eachItem in listView1.Items)
            {
                if (eachItem.SubItems[1].Text.Contains(text))
                {
                    eachItem.Selected = true;
                    listView1.Focus();
                }
            }
        }

        private void jobChanged(object sender, EventArgs e)
        {

            btn_populate.Enabled = true;
            foreach (ListViewItem eachItem in listView1.Items)
            {
                listView1.Items.Remove(eachItem);
            }
            btn_selected.Enabled = false;
            btn_export.Enabled = false;
        }

        private void btn_setup_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem eachItem in listView1.Items)
            {
                listView1.Items.Remove(eachItem);
            }

            Setup setup = new Setup();
            setup.Show();
        }
    }
}

