using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.Teslagrad2
{
    public partial class Teslagrad2Settings : UserControl
    {
        public List<SplitEntry> Splits { get; set; }
        public bool AutoReset { get; set; }

        private const int ROW_HEIGHT = 28;
        private readonly List<Panel> _rowPanels = new List<Panel>();

        private int _dragIndex = -1;
        private int _dropIndex = -1;
        private Rectangle _dragBox;
        private Font _headerFont;

        public Teslagrad2Settings()
        {
            Splits = new List<SplitEntry>
            {
                new SplitEntry(SplitType.StartTimer),
                new SplitEntry(SplitType.Elenor)
            };
            AutoReset = true;
            InitializeComponent();
            WireEvents();
            RebuildRows();
        }

        private void WireEvents()
        {
            chkAutoReset.CheckedChanged += (s, e) => AutoReset = chkAutoReset.Checked;
            btnAdd.Click += OnAdd;
            pnlRows.DragOver += OnPanelDragOver;
            pnlRows.DragDrop += OnPanelDragDrop;
            pnlRows.DragLeave += OnPanelDragLeave;
            this.Load += (s, e) => FitToParent();
            this.ParentChanged += (s, e) => FitToParent();
            this.SizeChanged += (s, e) => LayoutPanels();
        }

        private Control _subscribedParent;

        private void FitToParent()
        {
            if (_subscribedParent != null)
                _subscribedParent.SizeChanged -= OnParentSizeChanged;

            _subscribedParent = Parent;

            if (Parent != null)
            {
                Size = Parent.ClientSize;
                Parent.SizeChanged += OnParentSizeChanged;
            }
            LayoutPanels();
        }

        private void OnParentSizeChanged(object sender, EventArgs e)
        {
            if (Parent != null)
                Size = Parent.ClientSize;
            LayoutPanels();
        }

        private void LayoutPanels()
        {
            pnlTop.Width = ClientSize.Width;
            pnlRows.Location = new Point(0, pnlTop.Bottom);
            pnlRows.Size = new Size(ClientSize.Width, ClientSize.Height - pnlTop.Bottom);
        }

        #region Row Building

        private void RebuildRows()
        {
            pnlRows.SuspendLayout();

            foreach (var row in _rowPanels)
            {
                pnlRows.Controls.Remove(row);
                row.Dispose();
            }
            _rowPanels.Clear();

            for (int i = 0; i < Splits.Count; i++)
            {
                var row = CreateRow(i);
                _rowPanels.Add(row);
                pnlRows.Controls.Add(row);
            }

            UpdateRowPositions();
            dropIndicator.BringToFront();
            pnlRows.ResumeLayout();
        }

        private void UpdateRowPositions()
        {
            for (int i = 0; i < _rowPanels.Count; i++)
            {
                var row = _rowPanels[i];
                row.Tag = i;
                row.Location = new Point(0, i * ROW_HEIGHT);
                foreach (Control ctrl in row.Controls)
                {
                    if (ctrl.Tag is int)
                        ctrl.Tag = i;
                }
            }
        }

        private Panel CreateRow(int index)
        {
            var row = new Panel { Height = ROW_HEIGHT, Width = 380, Tag = index };
            PopulateRowControls(row, index);
            return row;
        }

        private void RefreshRowControls(int index)
        {
            var row = _rowPanels[index];
            row.SuspendLayout();
            foreach (Control ctrl in row.Controls)
                ctrl.Dispose();
            row.Controls.Clear();
            PopulateRowControls(row, index);
            row.ResumeLayout();
        }

        private void PopulateRowControls(Panel row, int index)
        {
            var entry = Splits[index];
            bool isStart = index == 0;
            bool isEnd = index == Splits.Count - 1;
            bool isDraggable = !isStart && !isEnd;

            int x = 4;

            if (isDraggable)
            {
                var handle = new Label
                {
                    Text = "\u2261",
                    Location = new Point(x, 4),
                    Size = new Size(18, 20),
                    Cursor = Cursors.SizeAll,
                    ForeColor = Color.Gray,
                    Tag = index
                };
                handle.MouseDown += OnHandleMouseDown;
                handle.MouseMove += OnHandleMouseMove;
                row.Controls.Add(handle);
            }
            x += 20;

            if (isStart)
            {
                var label = new Label
                {
                    Text = "Start Timer",
                    Location = new Point(x, 5),
                    AutoSize = true,
                    ForeColor = Color.Gray
                };
                row.Controls.Add(label);
            }
            else
            {
                var combo = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    DrawMode = DrawMode.OwnerDrawFixed,
                    Location = new Point(x, 3),
                    Size = new Size(170, 21),
                    Tag = index
                };
                PopulateCombo(combo);
                SelectComboType(combo, entry.Type);
                combo.DrawItem += OnComboDrawItem;
                combo.SelectedIndexChanged += OnRowComboChanged;
                combo.MouseWheel += OnComboMouseWheel;
                row.Controls.Add(combo);
                x += 175;

                if (entry.Type == SplitType.Scrolls)
                {
                    var lbl = new Label
                    {
                        Text = "ID:",
                        Location = new Point(x, 5),
                        AutoSize = true
                    };
                    row.Controls.Add(lbl);
                    x += 24;

                    var nud = new NumericUpDown
                    {
                        Location = new Point(x, 3),
                        Size = new Size(50, 21),
                        Minimum = 1,
                        Maximum = 100,
                        Value = Math.Max(1, Math.Min(100, entry.ScrollId)),
                        Tag = index
                    };
                    nud.ValueChanged += OnRowScrollIdChanged;
                    row.Controls.Add(nud);
                    x += 55;
                }
                else if (entry.Type == SplitType.SceneEntered)
                {
                    var txt = new TextBox
                    {
                        Text = entry.SceneName ?? "",
                        Location = new Point(x, 3),
                        Size = new Size(120, 21),
                        Tag = index
                    };
                    txt.Leave += OnRowSceneNameChanged;
                    row.Controls.Add(txt);
                    x += 125;
                }

                if (isDraggable)
                {
                    var btn = new Button
                    {
                        Text = "\u00D7",
                        Location = new Point(x, 2),
                        Size = new Size(24, 22),
                        Tag = index,
                        FlatStyle = FlatStyle.Flat,
                        ForeColor = Color.Gray,
                        Cursor = Cursors.Hand
                    };
                    btn.FlatAppearance.BorderSize = 0;
                    btn.Click += OnRowRemove;
                    row.Controls.Add(btn);
                }
            }
        }

        private void PopulateCombo(ComboBox combo)
        {
            string lastCategory = null;
            foreach (SplitType t in Enum.GetValues(typeof(SplitType)))
            {
                if (t == SplitType.StartTimer) continue;
                string cat = t.GetCategory();
                if (cat != lastCategory)
                {
                    combo.Items.Add(new SplitTypeItem(cat));
                    lastCategory = cat;
                }
                combo.Items.Add(new SplitTypeItem(t));
            }
        }

        private void SelectComboType(ComboBox combo, SplitType type)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                var item = (SplitTypeItem)combo.Items[i];
                if (!item.IsHeader && item.Type == type)
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
        }

        #endregion

        #region Row Event Handlers

        private void OnRowComboChanged(object sender, EventArgs e)
        {
            var combo = (ComboBox)sender;
            int index = (int)combo.Tag;
            if (index < 0 || index >= Splits.Count) return;
            if (combo.SelectedItem == null) return;

            var item = (SplitTypeItem)combo.SelectedItem;

            if (item.IsHeader)
            {
                for (int i = combo.SelectedIndex + 1; i < combo.Items.Count; i++)
                {
                    var next = (SplitTypeItem)combo.Items[i];
                    if (!next.IsHeader)
                    {
                        combo.SelectedIndex = i;
                        return;
                    }
                }
                return;
            }

            var oldType = Splits[index].Type;
            Splits[index].Type = item.Type;
            if (item.Type == SplitType.Scrolls && Splits[index].ScrollId == 0)
                Splits[index].ScrollId = 1;
            if (item.Type != SplitType.Scrolls)
                Splits[index].ScrollId = 0;
            if (item.Type != SplitType.SceneEntered)
                Splits[index].SceneName = "";

            bool hadExtra = oldType == SplitType.Scrolls || oldType == SplitType.SceneEntered;
            bool needsExtra = item.Type == SplitType.Scrolls || item.Type == SplitType.SceneEntered;
            if (hadExtra || needsExtra)
                RefreshRowControls(index);
        }

        private void OnRowScrollIdChanged(object sender, EventArgs e)
        {
            var nud = (NumericUpDown)sender;
            int index = (int)nud.Tag;
            if (index < 0 || index >= Splits.Count) return;
            Splits[index].ScrollId = (int)nud.Value;
        }

        private void OnRowSceneNameChanged(object sender, EventArgs e)
        {
            var txt = (TextBox)sender;
            int index = (int)txt.Tag;
            if (index < 0 || index >= Splits.Count) return;
            Splits[index].SceneName = txt.Text.Trim();
        }

        private void OnRowRemove(object sender, EventArgs e)
        {
            var btn = (Button)sender;
            int index = (int)btn.Tag;
            if (index <= 0 || index >= Splits.Count - 1) return;

            Splits.RemoveAt(index);

            pnlRows.SuspendLayout();
            var row = _rowPanels[index];
            pnlRows.Controls.Remove(row);
            row.Dispose();
            _rowPanels.RemoveAt(index);
            UpdateRowPositions();
            pnlRows.ResumeLayout();
        }

        private void OnComboMouseWheel(object sender, MouseEventArgs e)
        {
            if (e is HandledMouseEventArgs he)
                he.Handled = true;
        }

        #endregion

        #region Categorized ComboBox Rendering

        private void OnComboDrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            var combo = (ComboBox)sender;
            var item = (SplitTypeItem)combo.Items[e.Index];
            bool isEditArea = (e.State & DrawItemState.ComboBoxEdit) != 0;

            e.DrawBackground();

            if (item.IsHeader)
            {
                if (_headerFont == null || _headerFont.FontFamily.Name != e.Font.FontFamily.Name || _headerFont.Size != e.Font.Size)
                {
                    _headerFont?.Dispose();
                    _headerFont = new Font(e.Font, FontStyle.Bold);
                }
                e.Graphics.DrawString(item.DisplayText, _headerFont, Brushes.Gray,
                    e.Bounds.X + 2, e.Bounds.Y + 1);
            }
            else
            {
                var brush = (e.State & DrawItemState.Selected) != 0
                    ? SystemBrushes.HighlightText
                    : SystemBrushes.WindowText;
                string display = isEditArea ? item.DisplayText : "   " + item.DisplayText;
                e.Graphics.DrawString(display, e.Font, brush,
                    e.Bounds.X + 2, e.Bounds.Y + 1);
            }

            e.DrawFocusRectangle();
        }

        #endregion

        #region Drag and Drop

        private void OnHandleMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            var handle = (Label)sender;
            _dragIndex = (int)handle.Tag;

            var dragSize = SystemInformation.DragSize;
            var screenPos = handle.PointToScreen(e.Location);
            _dragBox = new Rectangle(
                screenPos.X - dragSize.Width / 2,
                screenPos.Y - dragSize.Height / 2,
                dragSize.Width, dragSize.Height);
        }

        private void OnHandleMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || _dragBox == Rectangle.Empty) return;

            var handle = (Label)sender;
            var screenPos = handle.PointToScreen(e.Location);
            if (!_dragBox.Contains(screenPos))
            {
                pnlRows.DoDragDrop(_dragIndex, DragDropEffects.Move);
                _dragBox = Rectangle.Empty;
            }
        }

        private void OnPanelDragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
            Point clientPoint = pnlRows.PointToClient(new Point(e.X, e.Y));
            int scrollOffset = pnlRows.VerticalScroll.Value;
            int targetIndex = (clientPoint.Y + scrollOffset + ROW_HEIGHT / 2) / ROW_HEIGHT;

            targetIndex = Math.Max(1, Math.Min(targetIndex, Splits.Count - 1));

            if (_dropIndex != targetIndex)
            {
                _dropIndex = targetIndex;
                dropIndicator.Location = new Point(0, targetIndex * ROW_HEIGHT - scrollOffset - 1);
                dropIndicator.Visible = true;
            }
        }

        private void OnPanelDragDrop(object sender, DragEventArgs e)
        {
            dropIndicator.Visible = false;

            if (_dragIndex < 1 || _dropIndex < 1 || _dragIndex == _dropIndex)
            {
                _dragIndex = -1;
                _dropIndex = -1;
                return;
            }

            var entry = Splits[_dragIndex];
            Splits.RemoveAt(_dragIndex);
            int insertAt = _dropIndex;
            if (_dragIndex < _dropIndex)
                insertAt--;
            insertAt = Math.Max(1, Math.Min(insertAt, Splits.Count - 1));
            Splits.Insert(insertAt, entry);

            var row = _rowPanels[_dragIndex];
            _rowPanels.RemoveAt(_dragIndex);
            _rowPanels.Insert(insertAt, row);

            pnlRows.SuspendLayout();
            UpdateRowPositions();
            pnlRows.ResumeLayout();

            _dragIndex = -1;
            _dropIndex = -1;
        }

        private void OnPanelDragLeave(object sender, EventArgs e)
        {
            _dropIndex = -1;
            dropIndicator.Visible = false;
        }

        #endregion

        #region Add

        private void OnAdd(object sender, EventArgs e)
        {
            int insertAt = Splits.Count - 1;
            Splits.Insert(insertAt, new SplitEntry(SplitType.Blink));

            pnlRows.SuspendLayout();
            var row = CreateRow(insertAt);
            _rowPanels.Insert(insertAt, row);
            pnlRows.Controls.Add(row);
            UpdateRowPositions();
            dropIndicator.BringToFront();
            pnlRows.ResumeLayout();
        }

        #endregion

        #region Serialization

        public XmlNode GetSettings(XmlDocument document)
        {
            var parent = document.CreateElement("Settings");

            var autoReset = document.CreateElement("AutoReset");
            autoReset.InnerText = AutoReset.ToString();
            parent.AppendChild(autoReset);

            var splitsNode = document.CreateElement("Splits");
            foreach (var entry in Splits)
            {
                var splitNode = document.CreateElement("Split");
                splitNode.InnerText = entry.Type.ToString();
                if (entry.Type == SplitType.Scrolls)
                    splitNode.SetAttribute("ScrollId", entry.ScrollId.ToString());
                if (entry.Type == SplitType.SceneEntered)
                    splitNode.SetAttribute("SceneName", entry.SceneName ?? "");
                splitsNode.AppendChild(splitNode);
            }
            parent.AppendChild(splitsNode);

            return parent;
        }

        public void SetSettings(XmlNode node)
        {
            var autoResetNode = node["AutoReset"];
            AutoReset = autoResetNode != null && bool.TryParse(autoResetNode.InnerText, out bool ar) ? ar : true;

            var splitsNode = node["Splits"];
            if (splitsNode != null && splitsNode.ChildNodes.Count >= 2)
            {
                Splits.Clear();
                foreach (XmlNode child in splitsNode.ChildNodes)
                {
                    if (Enum.TryParse(child.InnerText, out SplitType type))
                    {
                        int scrollId = 0;
                        var scrollAttr = child.Attributes?["ScrollId"];
                        if (scrollAttr != null)
                            int.TryParse(scrollAttr.Value, out scrollId);

                        string sceneName = "";
                        var sceneAttr = child.Attributes?["SceneName"];
                        if (sceneAttr != null)
                            sceneName = sceneAttr.Value;

                        Splits.Add(new SplitEntry(type, scrollId, sceneName));
                    }
                }

                if (Splits.Count == 0 || Splits[0].Type != SplitType.StartTimer)
                    Splits.Insert(0, new SplitEntry(SplitType.StartTimer));
                if (Splits.Count < 2)
                    Splits.Add(new SplitEntry(SplitType.Elenor));
            }

            chkAutoReset.Checked = AutoReset;
            RebuildRows();
        }

        #endregion
    }

    public class SplitTypeItem
    {
        public SplitType Type { get; }
        public string DisplayText { get; }
        public bool IsHeader { get; }

        public SplitTypeItem(SplitType type)
        {
            Type = type;
            DisplayText = type.GetDisplayName();
            IsHeader = false;
        }

        public SplitTypeItem(string categoryHeader)
        {
            DisplayText = "\u2500\u2500 " + categoryHeader + " \u2500\u2500";
            IsHeader = true;
        }

        public override string ToString() { return DisplayText; }
    }
}
