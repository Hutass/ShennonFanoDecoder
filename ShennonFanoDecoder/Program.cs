using System;
using System.Text;
using static Program;

internal class Program
{
    static void Main(string[] args)
    {
        ShennonFano decoder = new ShennonFano();
        decoder.Decode(
            "C:\\Users\\Артем\\Downloads\\test_txt (1).dat");
    }

    public class ShennonFano
    {
        public struct SignSheet
        {
            public byte value { get; set; }
            public int count { get; set; }

            public SignSheet(byte value)
            {
                this.value = value;
                this.count = 0;
            }
            public SignSheet()
            {
                this.value = 0;
                this.count = 0;
            }

        }
        private TreeNode<SignSheet> CodeTree { get; set; }

        public void Decode(string inputPath)
        {
            FileStream inputFile = File.OpenRead(inputPath);
            BinaryReader inputReader = new BinaryReader(inputFile);
            CodeTree = BuildTree(BuildTable(inputReader));
            string outputPath = inputPath.Split('.')[0];
            Decode(inputReader, outputPath);
            inputFile.Close();
        }

        private List<SignSheet> BuildTable(BinaryReader inputFileStream)
        {
            byte[] byteSign = new byte[1];
            List<SignSheet> signSheets = new List<SignSheet>();
            SignSheet sheet;
            for (int i = 0; i < 256; i++)
            {
                signSheets.Add(new SignSheet(Convert.ToByte(i)));
            }
            try
            {
                for (int i = 0; i < 256; i++)
                {
                    inputFileStream.Read(byteSign, 0, 1);
                    sheet = signSheets[i];
                    sheet.count += Convert.ToInt32(byteSign[0]);
                    signSheets[i] = sheet;
                }
                for (int i = signSheets.Count - 1; i >= 0; i--)
                {
                    if (signSheets[i].count == 0)
                    {
                        signSheets.Remove(signSheets[i]);
                    }
                }
                Sorter sorter= new Sorter();
                sorter.QuickSort(signSheets);
                //signSheets.Sort(((sign1, sign2) => sign2.count.CompareTo(sign1.count)));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Build Table Error");
                Console.WriteLine("{0}", ex.ToString());
            }

            return signSheets;
        }

        private TreeNode<SignSheet> BuildTree(List<SignSheet> table)
        {
            TreeNode<SignSheet> tree = new TreeNode<SignSheet>(new SignSheet());
            if (table.Count == 2)
            {
                tree.AddChild(table[0], TREE_LEFT);
                tree.AddChild(table[1], TREE_RIGHT);
            }
            else if (table.Count < 2)
            {
                tree.Value = table[0];
            }
            else
            {
                int sum = 0;
                int min = int.MaxValue;
                foreach (SignSheet t in table)
                {
                    sum += t.count;
                }

                int leftSum = 0;
                int rightSum = sum;
                for (int i = 0; i < table.Count; i++)
                {
                    leftSum += table[i].count;
                    rightSum -= table[i].count;

                    int diff = Math.Abs(leftSum - rightSum);
                    if (diff > min)
                    {
                        i--;
                        List<SignSheet> leftTable = new List<SignSheet>();
                        List<SignSheet> rightTable = new List<SignSheet>();
                        for (int j = 0; j < table.Count; j++)
                        {
                            if (j <= i)
                            {
                                leftTable.Add(table[j]);
                            }
                            else
                            {
                                rightTable.Add(table[j]);
                            }
                        }

                        tree.AddChild(new SignSheet(), TREE_LEFT);
                        tree.AddChild(new SignSheet(), TREE_RIGHT);
                        tree.Left = BuildTree(leftTable);
                        tree.Right = BuildTree(rightTable);
                        break;
                    }
                    else
                    {
                        min = diff;
                    }
                }
            }
            return tree;
        }

        private void Decode(BinaryReader inputFileStream, string outputPath)
        {
            byte infoByte = inputFileStream.ReadByte();
            int fileExtensionCrop = infoByte & 0x1F;
            int endOfFileCrop = (infoByte >> 5) & 0x7;
            outputPath = ExtensionRecovery(outputPath, fileExtensionCrop);
            FileStream outputFile = File.Open(outputPath, FileMode.Create);
            BinaryWriter outputWriter = new BinaryWriter(outputFile);
            byte[] byteSign = new byte[2];
            TreeNode<SignSheet> treeNodePointer = CodeTree;
            try
            {
                outputFile.Seek(0, SeekOrigin.End);
                inputFileStream.Read(byteSign, 0, 1);
                while (inputFileStream.Read(byteSign, 1, 1) != 0)
                {
                    for (int bitNumber = 8; bitNumber > 0; bitNumber--)
                    {
                        if ((byteSign[0] & (1 << bitNumber - 1)) == 0)
                        {
                            treeNodePointer = treeNodePointer[TREE_LEFT];
                        }
                        else
                        {
                            treeNodePointer = treeNodePointer[TREE_RIGHT];
                        }
                        if (treeNodePointer[TREE_LEFT] == null && treeNodePointer[TREE_RIGHT] == null)
                        {
                            outputWriter.Write(treeNodePointer.Value.value);
                            treeNodePointer = CodeTree;
                        }
                    }
                    byteSign[0] = byteSign[1];
                }
                for (int bitNumber = 8; bitNumber > endOfFileCrop; bitNumber--)
                {
                    if ((byteSign[0] & (1 << bitNumber - 1)) == 0)
                    {
                        treeNodePointer = treeNodePointer[TREE_LEFT];
                    }
                    else
                    {
                        treeNodePointer = treeNodePointer[TREE_RIGHT];
                    }
                    if (treeNodePointer[TREE_LEFT] == null && treeNodePointer[TREE_RIGHT] == null)
                    {
                        outputWriter.Write(treeNodePointer.Value.value);
                        treeNodePointer = CodeTree;
                    }
                }
                outputFile.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Decode Error\n");
                Console.WriteLine("{0}", ex.ToString());
            }
        }

        private string ExtensionRecovery(string unchangedPath, int fileExtensionCrop)
        {

            if (unchangedPath[unchangedPath.Length - 1] == ')')
            {
                int deleteStartIndex = 0;
                for(int i = unchangedPath.Length - 2; i > 0; i--)
                {
                    if (unchangedPath[i] == '(')
                    {
                        deleteStartIndex = i;
                        break;
                    }
                }
                unchangedPath = unchangedPath.Remove(deleteStartIndex - 1, unchangedPath.Length - deleteStartIndex + 1);
            }

            unchangedPath = unchangedPath.Remove(unchangedPath.Length - fileExtensionCrop - 1, 1);
            unchangedPath = unchangedPath.Insert(unchangedPath.Length - fileExtensionCrop, ".");

            return unchangedPath;
        }


        #region Sorter
        public class Sorter
        {
            public void QuickSort(List<SignSheet> sortedArray)
            {
                int leftBorder = 0;
                int rightBorder = sortedArray.Count - 1;
                QuickSort(sortedArray, leftBorder, rightBorder);
            }

            public void QuickSort(List<SignSheet> sortedArray, int leftBorder, int rightBorder)
            {
                int leftBuffer = leftBorder;
                int rightBuffer = rightBorder;
                var buffer = sortedArray[leftBuffer].count;
                while (leftBuffer <= rightBuffer)
                {
                    while (sortedArray[leftBuffer].count > buffer)
                    {
                        leftBuffer++;
                    }

                    while (sortedArray[rightBuffer].count < buffer)
                    {
                        rightBuffer--;
                    }

                    if (leftBuffer <= rightBuffer)
                    {
                        var temp = sortedArray[leftBuffer];
                        sortedArray[leftBuffer] = sortedArray[rightBuffer];
                        sortedArray[rightBuffer] = temp;
                        leftBuffer++;
                        rightBuffer--;
                    }
                }

                if (leftBorder < rightBuffer)
                    QuickSort(sortedArray, leftBorder, rightBuffer);
                if (leftBuffer < rightBorder)
                    QuickSort(sortedArray, leftBuffer, rightBorder);
            }          
        }
        #endregion

        #region Tree
        public static bool TREE_LEFT = true;
        public static bool TREE_RIGHT = false;

        public class TreeNode<T>
        {
            private T _value;
            private TreeNode<T> _left;
            private TreeNode<T> _right;
            private int _count;

            public TreeNode(T value)
            {
                _value = value;
                _count = 0;
            }

            public TreeNode<T> this[bool trig]
            {
                get
                {
                    if (trig) return _left;
                    else return _right;
                }
            }

            public TreeNode<T> Root { get; private set; }
            public TreeNode<T> Left { get => _left; set { _left = value; _left.Count = _count + 1; } }
            public TreeNode<T> Right { get => _right; set { _right = value; _right.Count = _count + 1; } }
            public int Count { get => _count; private set => _count = value; }

            public T Value { get => _value; set => _value = value; }

            public virtual TreeNode<T> AddChild(T value, bool trig)
            {
                var node = new TreeNode<T>(value) { Root = this };
                node.Count = _count + 1;
                if (trig)
                    _left = node;
                else
                    _right = node;
                return node;
            }
            public virtual TreeNode<T> AddChild(TreeNode<T> value, bool trig)
            {
                value.Root = this;
                var node = value;
                node.Count = _count + 1;
                if (trig)
                    _left = node;
                else
                    _right = node;
                return node;
            }

            public bool RemoveChild(TreeNode<T> node, bool trig)
            {
                if (trig)
                {
                    if (_left != null)
                    { _left = null; return true; }
                    else
                        return false;
                }
                else
                {
                    if (_right != null)
                    { _right = null; return true; }
                    else
                        return false;
                }

            }

            public void ActionDo(Action<T> action)
            {
                action(Value);
                if (_left != null) _left.ActionDo(action);
                if (_right != null) _right.ActionDo(action);
            }
            public void ActionNodeDo(Action<TreeNode<T>> action)
            {
                action(this);
                if (_left != null) _left.ActionNodeDo(action);
                if (_right != null) _right.ActionNodeDo(action);
            }

        }
        #endregion
    }
}