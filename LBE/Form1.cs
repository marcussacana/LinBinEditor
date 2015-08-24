using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace LBE
{
    public partial class Form1 : Form
    {
        public Form1(string[] Args)
        {
            InitializeComponent();
            title = "LBE - LinBinEditor";
            try {
                if (System.IO.File.Exists(Args[0]))
                {
                    openFileDialog1.FileName = Args[0];
                    openFileDialog1_FileOk(null, null);
                }
            } catch { }
            ReplaceJapChars.Appearance = Appearance.Button;
            this.AcceptButton = button2;
        }

        private void lbe_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }
        public bool ContainsBlankValueInBlankOffset = false;
        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            panel1.Visible = false;
            panel2.Visible = true;
            this.MaximizeBox = true;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            dialogs.Items.Clear();
            Script = new string[0];
            ContainsBlankValueInBlankOffset = false;
            ScriptTreeOffsetPos = -1;
            Dialogues = new Dialog[0];
            OriginalDlgs = new Dialog[0];
            new Thread(BinReader).Start();
        }
        string[] Script = new string[0];
        private void BinReader()
        {
            string fileName = openFileDialog1.FileName;
            Script = Tools.ByteArrayToString(System.IO.File.ReadAllBytes(fileName)).Split('-');
            Dialogues = new Dialog[0];
            OriginalDlgs = new Dialog[0];
            bool inDlg = false;
            int count = 0;
            int DlgIndex = 0;
            int DlgEndIndex = 0;
            bool UnlimitedSize = false;
            for (int i = 0; i < Script.Length - 1; i++)
            {
                title = "Processing Script " + (i + 1) + "/" + Script.Length + " (" + ((i + 1) * 100 / Script.Length) + "%)";
                if (inDlg)
                    count++;
                if (count > 150 && !UnlimitedSize)
                {
                    count = 0;
                    i = DlgIndex + 1;
                    DlgIndex = 0;
                    inDlg = false;
                    continue;
                }
                if (Script[i] + Script[i + 1] == "FFFE")
                {
                    if (inDlg)
                    {
                        count = 0;
                        i = DlgIndex + 1;
                        DlgIndex = 0;
                        inDlg = false;
                        continue;
                    }
                    count = 0;
                    inDlg = true;
                    DlgIndex = i;
                    DlgEndIndex = 0;
                    continue;
                }
                bool condition = false;
                if (!(i + 3 >= Script.Length))
                    if (Script[i] + Script[i + 1] == "0000" && Script[i + 2] + Script[i + 3] == "FFFE")
                        condition = true;
                    else
                        condition = false;
                else
                {
                    if (Script[i] + Script[i + 1] == "0000" && Script[i - 1] != "0A")
                        condition = true;
                    else
                        condition = false;
                }
                if (condition && inDlg)
                {
                    DlgEndIndex = i;
                    inDlg = false;
                    if (UnlimitedSize == false)
                    {
                        TreeID = Script[DlgIndex - 4] + " " + Script[DlgIndex - 3] + " " + Script[DlgIndex - 2] + " " + Script[DlgIndex - 1];
                        UnlimitedSize = true;
                    }
                    addDialog(DlgIndex, DlgEndIndex, Script);
                    continue;
                }
            }
            string Hex = Tools.IntToHex(Dialogues.Length).Replace(@" ", "");
            if (Dialogues.Length != 0)
                for (int i = Dialogues[0].StartPos; i > 0; i--)
                {
                    if (Script[i] + Script[i + 1] + Script[i + 2] + Script[i + 3] == Hex)
                        ScriptTreeOffsetPos = i;
                }
            else
                MessageBox.Show("Don't exist dialogues in this file,\nor this format don't is suported by LBE", "Failed to open", MessageBoxButtons.OK, MessageBoxIcon.Error);

            title = "LBE";
            SaveEnable = true;
        }
        public string TreeID = "";
        public int ScriptTreeOffsetPos = -1;
        private void addDialog(int DlgIndex, int DlgEndIndex, string[] Script)
        {
            string Dialogue = "";
            for (int ind = DlgIndex + 2; ind < DlgEndIndex; ind += 2)
            {
                Dialogue += Script[ind] + Script[ind+1] + "-";
            }
            string breakline = Tools.UnicodeHexToUnicodeString("0A 00");
            Dialogue = Tools.UnicodeHexToUnicodeString(Dialogue.Replace("-", "")).Replace(breakline, @"\n").Replace("\r", "");
            Dialog[] temp = new Dialog[Dialogues.Length + 1];
            Dialogues.CopyTo(temp, 0);
            Dialog dialog = new Dialog();
            dialog.Content = Dialogue;
            dialog.StartPos = DlgIndex;
            dialog.EndPos = DlgEndIndex;
            if (dialog.Content.EndsWith(@"\n"))
            {
                dialog.AppendLineBreak = true;
                dialog.Content = dialog.Content.Substring(0, dialog.Content.Length-2);
            }
            if (dialog.Content.EndsWith(@"\n<CLT>"))
            {
                dialog.AppendCLTLineBreak = true;
                dialog.Content = dialog.Content.Substring(0, dialog.Content.Length - 7) + "<CLT>";
            }
            temp[Dialogues.Length] = dialog;
            Dialogues = temp;
        }
        public Dialog[] OriginalDlgs = new Dialog[0];
        public Dialog[] Dialogues = new Dialog[0];
        private string title;
        public byte[] file = new byte[0];
        private void timer1_Tick(object sender, EventArgs e)
        {
            this.Text = title;
            if (title == "LBE")
            {
                title = "LBE - Script Loaded";
                OriginalDlgs = Dialogues;
                foreach (Dialog dl in Dialogues)
                {
                    dialogs.Items.Add(dl.Content);

                }            
            }
            if (Generated)
            {
                Generated = false;
                if (Test(NewScript, ScriptTreeOffsetPos))
                {
                    SaveFileDialog sfd = new SaveFileDialog();
                    sfd.Title = "Save as..";
                    sfd.Filter = "All Danganronpa Script files |*.lin";
                    sfd.FileName = System.IO.Path.GetFileName(openFileDialog1.FileName);
                    DialogResult dr = sfd.ShowDialog();
                    if (dr == DialogResult.OK)
                    {
                        System.IO.File.WriteAllBytes(sfd.FileName, file);
                    }
                }
                else { MessageBox.Show("Ops, your file have incorrect offsets...\nPlease try save again...", "LBE - Fail to Save", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }
            button1.Enabled = SaveEnable;
        }

        private bool Test(string[] file, int TreeOffsetPos)
        {
            int Postion = TreeOffsetPos;
            int count = Tools.HexToInt(GetOffset(file, TreeOffsetPos));
            if (count != Dialogues.Length)
                return false;
            Next:;
            Postion += 4;
            int offset = Tools.HexToInt(GetOffset(file, Postion)) + TreeOffsetPos;
            if (offset >= Dialogues[0].StartPos)
                return true;

            title = "LBE - Testing File: " + offset + "/" + Dialogues[0].StartPos;
            if (file[offset] + file[offset + 1] != "FFFE")
                return false;
            goto Next;
        }

        private string GetOffset(string[] file, int treeOffsetPos)
        {
            try
            {
                return file[treeOffsetPos + 3] + file[treeOffsetPos + 2] + file[treeOffsetPos + 1] + file[treeOffsetPos];
            }
            catch { return "0000"; }

        }


        private void DialogContent_KeyPress(object sender, KeyPressEventArgs e)
        {
        }

        private void bntNext_Click(object sender, EventArgs e)
        {
            try
            {
                dialogs.SelectedIndex += 1;
            }
            catch { MessageBox.Show("This is the last line.", "LBE", MessageBoxButtons.OK, MessageBoxIcon.Information); }

        }
        public int LastIndex = 0;
        private void dialogs_SelectedIndexChanged(object sender, EventArgs e)
        {
            try {
                DialogContent.Enabled = true;
                DialogContent.Text = Dialogues[dialogs.SelectedIndex].Content;
                title = "LBE - Dialogue_ID=" + dialogs.SelectedIndex;
            }
            catch
            {
                DialogContent.Enabled = true;
                DialogContent.Text = Dialogues[LastIndex].Content;
                title = "LBE - Dialogue_ID=" + LastIndex;
            }
        }

        private void bntBack_Click(object sender, EventArgs e)
        {
            try
            {
                dialogs.SelectedIndex -= 1;
            }
            catch { MessageBox.Show("This is the first line.", "LBE", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        }
        public bool SaveEnable = false;
        private void button1_Click(object sender, EventArgs e)
        {
            SaveEnable = false;
            new Thread(GenerateScript).Start();
        }
        public string[] NewScript = new string[0];

        public string[] OffsetTree = new string[0];
        public void WriteOffset(string HexOffset)
        {
            string[] Offset = HexOffset.Split(' ');
            foreach (string hex in Offset)
            {
                title = "LBE - Appending: " + hex;
                string[] temp = new string[OffsetTree.Length + 1];
                OffsetTree.CopyTo(temp, 0);
                temp[OffsetTree.Length] = hex;
                OffsetTree = temp;
            }
        }
        private void GenerateScript()
        {

            OffsetTree = new string[0];
            NewScript = new string[0];
            int id = 0;
            int DialogPos = -1;
            bool allSaved = false;
            for (int offset = 0; offset < Script.Length; offset++)
            {
                title = "LBE - Generating New Script " + (offset+1) + "/" + Script.Length + " (" + (((offset+1)*100)/Script.Length) + "%)";
                if (offset == ScriptTreeOffsetPos)
                {
                    WriteOffset(Tools.IntToHex((Dialogues.Length)));
                }
                if (!allSaved)
                {
                    if (DialogPos == -1)
                    {
                        DialogPos = OriginalDlgs[id].StartPos;
                    }
                    if (offset == DialogPos)
                    {

                        int position = Dialogues[id].StartPos;
                        AddDialogue(Dialogues[id].Content, Dialogues[id].AppendLineBreak, Dialogues[id].AppendCLTLineBreak);
                        offset = OriginalDlgs[id].EndPos + 1;
                        string hex = Tools.IntToHex(position - ScriptTreeOffsetPos);
                        WriteOffset(hex);
                        id++;
                        if (id < Dialogues.Length)
                            DialogPos = Dialogues[id].StartPos;
                        if (id == Dialogues.Length)
                            allSaved = true;
                    }
                    else
                    {
                        appendScript(Script[offset]);
                    }
                }
                else
                {
                    appendScript(Script[offset]);
                }
            }
            int pos = 0;
            for (int i = ScriptTreeOffsetPos; i < Dialogues[0].StartPos; i++)
            {
                try {
                    if (pos >= OffsetTree.Length)
                        WriteOffset(TreeID);
                    NewScript[i] = OffsetTree[pos];
                    pos++;
                }
                catch { }
                }
            string bytes = "";
            foreach (string hex in NewScript)
                bytes += hex;
            file = Tools.StringToByteArray(bytes);
            Generated = true;
            title = "LBE - File Generated";
            SaveEnable = true;
        }
        public bool Generated = false;
        private void AddDialogue(string content, bool AppendLineBreak, bool AppendCLTLineBreak)
        {
            string str = content;
            string breakline = Tools.UnicodeHexToUnicodeString("0A00");
            if (AppendCLTLineBreak && str.EndsWith("<CLT>") && !str.EndsWith("\\n<CLT>"))
            {
                int length = str.Length;
                str = str.Substring(0, str.Length - 5) + "\\n<CLT>";
            }
            string Dialogue = Tools.UnicodeStringToHex(str.Replace(@"\n", breakline));
            if (AppendLineBreak)
                Dialogue = "FF FE " + Dialogue + " 0A 00 00 00";
            else
                Dialogue = "FF FE " + Dialogue + " 00 00";


            string[] hexs = Dialogue.Split(' ');
            int position = -1;
            foreach (string hex in hexs)
            {
                title = "LBE - Appending: " + hex;
                if (position != -1)
                    appendScript(hex);
                else
                    position = appendScript(hex);
            }
        }

        private int appendScript(string hex)
        {
            string[] temp = new string[NewScript.Length + 1];
            NewScript.CopyTo(temp, 0);
            temp[NewScript.Length] = hex;
            NewScript = temp;
            return NewScript.Length;
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            int size = (int)numericUpDown1.Value;
            this.DialogContent.Font = new Font("Microsoft Sans Serif", float.Parse(size.ToString())*1.7F, FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            DialogContent.Location = new Point(3, ((button1.Location.Y-(3*size)) - size*2)-10);
            DialogContent.Size = new Size(DialogContent.Size.Width, (int)(25F + (size*3)));
            dialogs.Size = new Size(dialogs.Size.Width, DialogContent.Location.Y-10);
            DialogContent_TextChanged(null, null);
        }
        bool resized = false;
        private void Form1_Resize(object sender, EventArgs e)
        {
            int size = (int)numericUpDown1.Value;
            if (size == numericUpDown1.Maximum)
                size--;
            else
            {
                if (size == numericUpDown1.Minimum)
                    size++;
                else
                {
                    if (resized)
                    {
                        resized = false;
                        size++;
                    }
                    else
                    {
                        resized = true;
                        size--;
                    }
                }
            }

            numericUpDown1.Value = size;
        }

        private void Form1_ResizeBegin(object sender, EventArgs e)
        {
           
        }

        private void saveFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            button1_Click(null, null);
        }

        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }
        private void DialogContent_TextChanged(object sender, EventArgs e)
        {
            string tokens = "<CLT(\\s?)([0-9]?)([0-9]?)>|(\\\\n)";

            bool OnlyFix = false;
            System.Text.RegularExpressions.Regex rex = new System.Text.RegularExpressions.Regex(tokens);
            System.Text.RegularExpressions.MatchCollection mc = rex.Matches(DialogContent.Text);
            int StartCursorPosition = DialogContent.SelectionStart;
            DialogContent.SelectAll();
            DialogContent.SelectionColor = Color.Black;
            string[] Changes = new string[mc.Count];
            string PureText = DialogContent.Text.Replace("\\n", "/n");
            DialogContent.SelectionFont = new Font(DialogContent.Font.Name, DialogContent.Font.Size, FontStyle.Regular, GraphicsUnit.Point, ((byte)(128)));
        again:;
            int pos = 0;            
            foreach (System.Text.RegularExpressions.Match m in mc)
            {
                int startIndex = m.Index;
                int StopIndex = m.Length;
                Changes[pos] = startIndex + ":" + StopIndex;
                PureText = PureText.Replace(m.Value, "");
                DialogContent.Select(startIndex, StopIndex);
                DialogContent.SelectionColor = Color.Blue;
                DialogContent.SelectionFont = new Font(DialogContent.Font.Name, DialogContent.SelectionFont.Size, FontStyle.Bold, GraphicsUnit.Point, ((byte)(128)));
                pos++;
            }
            if (DialogContent.Text.Length > 52 && !DialogContent.Text.Contains("\\n") && !OnlyFix)
            {
                int InScript = 0;
                int CorrectIndex = 0;
                for (int i = 0; CorrectIndex < 51; i++)
                {
                    bool IsText = ScritStringMerge(i, Changes);
                    if (IsText)
                        CorrectIndex++;
                    InScript++;
                }
                DialogContent.Select(InScript, DialogContent.Text.Length - InScript);
                DialogContent.SelectionColor = Color.Red;
                OnlyFix = true;
                goto again;
            }
            else if (DialogContent.Text.Length > 103 && !OnlyFix)
            {
                int InScript = 0;
                int CorrectIndex = 0;
                for (int i = 0; CorrectIndex < 101; i++)
                {
                    bool IsText = ScritStringMerge(i, Changes);
                    if (IsText)
                        CorrectIndex++;
                    InScript++;
                }
                DialogContent.Select(InScript, DialogContent.Text.Length - InScript);
                DialogContent.SelectionColor = Color.Red;
                OnlyFix = true;
                goto again;
            }
            DialogContent.Select(StartCursorPosition, 0);
        }

        private bool ScritStringMerge(int pos, string[] changes)
        {
            foreach (string change in changes)
            {
                string[] parse = change.Split(':');
                int start = int.Parse(parse[0]);
                int length = int.Parse(parse[1]);
                if ((pos >= start && pos <= start + length))
                {
                    return false;
                }
            }
            return true;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (ReplaceJapChars.Checked)
                DialogContent.Text = DialogContent.Text.Replace("?", "？").Replace(",", "、").Replace("!", "！");
            int id = int.Parse(title.Split('=')[1]);
            Dialogues[id].Content = DialogContent.Text;
            dialogs.Items[id] = Dialogues[id].Content;
            bntNext_Click(null, null);
        }
    }
    public class Dialog
    {
        public int StartPos = 0;
        public int EndPos = 0;
        public string Content = "";
        public bool AppendLineBreak = false;
        public bool AppendCLTLineBreak = false;
    }
    public class Tools
    {
        public static string IntToHex(int val)
        {
            string hexValue = val.ToString("X");
            if (hexValue.Length > 2)
            {
                if (hexValue.Length.ToString().EndsWith("1") || hexValue.Length.ToString().EndsWith("3") || hexValue.Length.ToString().EndsWith("5") || hexValue.Length.ToString().EndsWith("7") || hexValue.Length.ToString().EndsWith("9"))
                { hexValue = "0" + hexValue; }
                string NHEX = "";
                for (int index = hexValue.Length; index != 0; index -= 2)
                {
                    NHEX += hexValue.Substring(index - 2, 2) + " ";
                }
                NHEX = NHEX.Substring(0, NHEX.Length - 1);
                if (NHEX.Replace(@" ", "").Length == 2)
                    return NHEX + " 00 00 00";
                if (NHEX.Replace(@" ", "").Length == 4)
                    return NHEX + " 00 00";
                if (NHEX.Replace(@" ", "").Length == 5)
                    return NHEX + " 00";
                if (NHEX.Replace(@" ", "").Length == 6)
                    return NHEX;
                return "null";
            }
            else
            {
                if (hexValue.Length == 1)
                    return "0" + hexValue + " 00 00 00";
                return hexValue + " 00 00 00";
            }
        }

        public static string StringToHex(string _in)
        {
            string input = _in;
            char[] values = input.ToCharArray();
            string r = "";
            foreach (char letter in values)
            {
                int value = Convert.ToInt32(letter);
                string hexOutput = String.Format("{0:X}", value);
                if (value > 255)
                    return UnicodeStringToHex(input);
                r += value + " ";
            }
            string[] bytes = r.Split(' ');
            byte[] b = new byte[bytes.Length - 1];
            int index = 0;
            foreach (string val in bytes)
            {
                if (index == bytes.Length - 1)
                    break;
                if (int.Parse(val) > byte.MaxValue)
                {
                    MessageBox.Show("FAILED in char " + input.Substring(int.Parse(val), 1) + "\nChar ignored.", "FAILED ONE CHAR!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    b[index] = byte.Parse("0");
                }
                else
                    b[index] = byte.Parse(val);
                index++;
            }
            r = ByteArrayToString(b);
            return r.Replace("-", @" ");
        }
        public static string UnicodeStringToHex(string _in)
        { 
            string input = _in;
            char[] values = Encoding.Unicode.GetChars(Encoding.Unicode.GetBytes(input.ToCharArray()));
            string r = "";
            foreach (char letter in values)
            {
                int value = Convert.ToInt32(letter);
                string hexOutput = String.Format("{0:X}", value);
                r += value + " ";
            }
            UnicodeEncoding unicode = new UnicodeEncoding();
            byte[] b = unicode.GetBytes(input);
            r = ByteArrayToString(b);
            return r.Replace("-", @" ");
        }
        public static byte[] StringToByteArray(String hex)
        {
            try
            {
                int NumberChars = hex.Length;
                byte[] bytes = new byte[NumberChars / 2];
                for (int i = 0; i < NumberChars; i += 2)
                    bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                return bytes;
            }
            catch { MessageBox.Show("Invalid format file!", "LBE"); return new byte[0]; }
        }
        public static string ByteArrayToString(byte[] ba)
        {
            string hex = BitConverter.ToString(ba);
            return hex;
        }

        public static int HexToInt(string hex)
        {
            int num = Int32.Parse(hex, System.Globalization.NumberStyles.HexNumber);
            return num;
        }

        public static string HexToString(string hex)
        {
            string[] hexValuesSplit = hex.Split(' ');
            string returnvar = "";
            foreach (string hexs in hexValuesSplit)
            {
                int value = Convert.ToInt32(hexs, 16);
                char charValue = (char)value;
                returnvar += charValue;
            }
            return returnvar;
        }

        public static string UnicodeHexToUnicodeString(string hex)
        {
            string hexString = hex.Replace(@" ", "");
            int length = hexString.Length;
            byte[] bytes = new byte[length / 2];

            for (int i = 0; i < length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
            }
           
            return Encoding.Unicode.GetString(bytes);
        }

    }
}
