using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Outlook = Microsoft.Office.Interop.Outlook;
using System.Runtime.InteropServices;

namespace CopyPolish
{
    public class SearchForm : Form
    {
        private TreeView treeFolders;
        private TextBox txtSearch;
        private Button btnSearch;
        private DataGridView gridResults;
        private Label lblStatus;
        private SplitContainer splitContainer;
        private Panel topPanel;

        public SearchForm()
        {
            InitializeComponent();
            LoadFolders();
        }

        private void InitializeComponent()
        {
            this.Text = "Gelişmiş Arama - CopyPolish";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            // Top Panel for Search Input
            topPanel = new Panel { Dock = DockStyle.Top, Height = 60, Padding = new Padding(10) };
            
            Label lblSearch = new Label { Text = "Arama:", AutoSize = true, Location = new Point(10, 22) };
            txtSearch = new TextBox { Location = new Point(70, 18), Width = 600, Font = new Font("Segoe UI", 10) };
            txtSearch.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) PerformSearch(); };

            btnSearch = new Button { Text = "Ara", Location = new Point(680, 16), Width = 100, Height = 30, BackColor = Color.FromArgb(0, 120, 215), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSearch.Click += (s, e) => PerformSearch();

            topPanel.Controls.AddRange(new Control[] { lblSearch, txtSearch, btnSearch });

            // Split Container for Folders and Results
            splitContainer = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 250 };

            // Left Side: Folder Tree
            Label lblFolders = new Label { Text = "Klasörler", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 9, FontStyle.Bold), Padding = new Padding(5) };
            treeFolders = new TreeView { Dock = DockStyle.Fill, CheckBoxes = true };
            
            splitContainer.Panel1.Controls.Add(treeFolders);
            splitContainer.Panel1.Controls.Add(lblFolders);

            // Right Side: Results Grid
            gridResults = new DataGridView 
            { 
                Dock = DockStyle.Fill, 
                AllowUserToAddRows = false, 
                AllowUserToDeleteRows = false, 
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };
            gridResults.Columns.Add("Subject", "Konu");
            gridResults.Columns.Add("Sender", "Gönderen");
            gridResults.Columns.Add("Time", "Tarih");
            gridResults.Columns.Add("Folder", "Klasör");
            
            // Adjust column widths
            gridResults.Columns["Subject"].FillWeight = 40;
            gridResults.Columns["Sender"].FillWeight = 20;
            gridResults.Columns["Time"].FillWeight = 20;
            gridResults.Columns["Folder"].FillWeight = 20;

            gridResults.CellDoubleClick += GridResults_CellDoubleClick;

            splitContainer.Panel2.Controls.Add(gridResults);

            // Status Bar
            lblStatus = new Label { Dock = DockStyle.Bottom, Height = 25, Text = "Hazır", TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(5), BackColor = Color.WhiteSmoke };

            this.Controls.Add(splitContainer);
            this.Controls.Add(topPanel);
            this.Controls.Add(lblStatus);
        }

        private void LoadFolders()
        {
            try
            {
                Outlook.NameSpace ns = Globals.ThisAddIn.Application.GetNamespace("MAPI");
                foreach (Outlook.Folder folder in ns.Folders)
                {
                    AddFolderNode(folder, treeFolders.Nodes);
                }
                if (treeFolders.Nodes.Count > 0) treeFolders.Nodes[0].Expand();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Klasörler yüklenirken hata oluştu: " + ex.Message);
            }
        }

        private void AddFolderNode(Outlook.Folder folder, TreeNodeCollection parentCollection)
        {
            try
            {
                TreeNode node = parentCollection.Add(folder.EntryID, folder.Name);
                node.Tag = folder;

                // Default check Inbox
                if (folder.Name == "Inbox" || folder.Name == "Gelen Kutusu")
                {
                    node.Checked = true;
                }

                if (folder.Folders.Count > 0)
                {
                    foreach (Outlook.Folder subFolder in folder.Folders)
                    {
                        AddFolderNode(subFolder, node.Nodes);
                    }
                }
            }
            catch { /* Skip folders we can't access */ }
        }

        private void PerformSearch()
        {
            string query = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                MessageBox.Show("Lütfen aranacak bir metin girin.");
                return;
            }

            List<Outlook.Folder> selectedFolders = GetSelectedFolders(treeFolders.Nodes);
            if (selectedFolders.Count == 0)
            {
                MessageBox.Show("Lütfen en az bir klasör seçin.");
                return;
            }

            gridResults.Rows.Clear();
            lblStatus.Text = "Aranıyor...";
            Application.DoEvents();

            int resultCount = 0;

            try
            {
                foreach (var folder in selectedFolders)
                {
                    try
                    {
                        // Use Outlook Table for faster search
                        string filter = $"@SQL=\"urn:schemas:httpmail:subject\" LIKE '%{query}%' OR \"urn:schemas:httpmail:textdescription\" LIKE '%{query}%' OR \"urn:schemas:httpmail:fromname\" LIKE '%{query}%'";
                        
                        Outlook.Table table = null;
                        try
                        {
                            table = folder.GetTable(filter, Outlook.OlTableContents.olUserItems);
                        }
                        catch
                        {
                            // Fallback or skip if filter is invalid for folder type
                            continue;
                        }

                        table.Columns.Add("EntryID");
                        table.Columns.Add("Subject");
                        table.Columns.Add("SenderName");
                        table.Columns.Add("SentOn");

                        while (!table.EndOfTable)
                        {
                            Outlook.Row row = table.GetNextRow();
                            string subject = row["Subject"]?.ToString() ?? "(No Subject)";
                            string sender = row["SenderName"]?.ToString() ?? "";
                            string time = row["SentOn"]?.ToString() ?? "";
                            string entryId = row["EntryID"]?.ToString();

                            int rowIndex = gridResults.Rows.Add(subject, sender, time, folder.Name);
                            gridResults.Rows[rowIndex].Tag = new ItemLocation { EntryID = entryId, StoreID = folder.StoreID };
                            
                            resultCount++;
                        }
                        
                        if (table != null) Marshal.ReleaseComObject(table);
                    }
                    catch (Exception ex)
                    {
                        // Log error per folder but continue
                        Console.WriteLine($"Error searching folder {folder.Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Arama sırasında genel bir hata oluştu: " + ex.Message);
            }

            lblStatus.Text = $"Arama tamamlandı. {resultCount} sonuç bulundu.";
        }

        private List<Outlook.Folder> GetSelectedFolders(TreeNodeCollection nodes)
        {
            List<Outlook.Folder> list = new List<Outlook.Folder>();
            foreach (TreeNode node in nodes)
            {
                if (node.Checked && node.Tag is Outlook.Folder folder)
                {
                    list.Add(folder);
                }
                list.AddRange(GetSelectedFolders(node.Nodes));
            }
            return list;
        }

        private void GridResults_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var tag = gridResults.Rows[e.RowIndex].Tag as ItemLocation;
                if (tag != null)
                {
                    try
                    {
                        Outlook.NameSpace ns = Globals.ThisAddIn.Application.GetNamespace("MAPI");
                        object item = ns.GetItemFromID(tag.EntryID, tag.StoreID);
                        if (item is Outlook.MailItem mail)
                        {
                            mail.Display();
                        }
                        else if (item is Outlook.MeetingItem meeting)
                        {
                            meeting.Display();
                        }
                        else
                        {
                            // Try generic display
                            ((dynamic)item).Display();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Öğe açılırken hata oluştu: " + ex.Message);
                    }
                }
            }
        }

        private class ItemLocation
        {
            public string EntryID { get; set; }
            public string StoreID { get; set; }
        }
    }
}
