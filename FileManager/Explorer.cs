using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace FileManager
{
    public delegate void OnKey(ConsoleKeyInfo key);

    class Explorer
    {
        public static int HEIGHT_KEYS = 3;
        public static int BOTTOM_OFFSET = 2;

        public event OnKey KeyPress;
        List<FilePanel> panels = new List<FilePanel>();
        private int activePanelIndex;

        static Explorer()
        {
            Console.SetWindowSize(120, 40);
            Console.SetBufferSize(120, 40);
        }

        public Explorer()
        {
            FilePanel filePanel = new FilePanel();
            filePanel.Top = 0;
            filePanel.Left = 0;
            this.panels.Add(filePanel);

            filePanel = new FilePanel();
            filePanel.Top = FilePanel.PANEL_HEIGHT;
            filePanel.Left = 0;
            this.panels.Add(filePanel);

            activePanelIndex = 0;

            this.panels[this.activePanelIndex].Active = true;
            KeyPress += this.panels[this.activePanelIndex].KeyboardProcessing;

            foreach (FilePanel fp in panels)
            {
                fp.Show();
            }

            this.ShowKeys();
        }

        public void Run()
        {
            bool exit = false;
            while (!exit)
            {
                if (Console.KeyAvailable)
                {
                    this.ClearMessage();

                    ConsoleKeyInfo userKey = Console.ReadKey(true);
                    switch (userKey.Key)
                    {
                        case ConsoleKey.Tab:
                            this.ChangeActivePanel();
                            break;
                        case ConsoleKey.Enter:
                            this.ChangeDirectoryOrRunProcess();
                            break;
                        case ConsoleKey.F3:
                            this.ViewFile();
                            break;
                        case ConsoleKey.F4:
                            this.Copy();
                            break;
                        case ConsoleKey.F5:
                            this.Move();
                            break;
                        case ConsoleKey.F6:
                            this.CreateDirectory();
                            break;
                        case ConsoleKey.F7:
                            this.Rename();
                            break;
                        case ConsoleKey.F8:
                            this.Delete();
                            break;
                        case ConsoleKey.Escape:
                            exit = true;
                            Console.ResetColor();
                            Console.Clear();
                            break;
                        case ConsoleKey.DownArrow:
                            goto case ConsoleKey.PageUp;
                        case ConsoleKey.UpArrow:
                            goto case ConsoleKey.PageUp;
                        case ConsoleKey.End:
                            goto case ConsoleKey.PageUp;
                        case ConsoleKey.Home:
                            goto case ConsoleKey.PageUp;
                        case ConsoleKey.PageDown:
                            goto case ConsoleKey.PageUp;
                        case ConsoleKey.PageUp:
                            this.KeyPress(userKey);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private string AksName(string message)
        {
            string name;
            Console.CursorVisible = true;
            do
            {
                this.ClearMessage();
                this.ShowMessage(message);
                name = Console.ReadLine();
            } while (name.Length == 0);
            Console.CursorVisible = false;
            this.ClearMessage();
            return name;
        }

        private void Copy()
        {
            foreach (FilePanel panel in panels)
            {
                if (panel.isDiscs)
                {
                    return;
                }
            }

            if (this.panels[0].Path == this.panels[1].Path)
            {
                return;
            }

            try
            {
                string destPath = this.activePanelIndex == 0 ? this.panels[1].Path : this.panels[0].Path;

                FileSystemInfo fileObject = this.panels[this.activePanelIndex].GetActiveObject();
                FileInfo currentFile = fileObject as FileInfo;

                if (currentFile != null)
                {
                    string fileName = currentFile.Name;
                    string destName = Path.Combine(destPath, fileName);
                    File.Copy(currentFile.FullName, destName, true);
                }

                else
                {
                    string currentDir = ((DirectoryInfo)fileObject).FullName;
                    string destDir = Path.Combine(destPath, ((DirectoryInfo)fileObject).Name);
                    CopyDirectory(currentDir, destDir);
                }

                this.RefreshPannels();
            }
            catch (Exception e)
            {
                this.ShowMessage(e.Message);
                return;
            }
        }

        private void CopyDirectory(string sourceDirName, string destDirName)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
            }

            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(destDirName, subdir.Name);
                CopyDirectory(subdir.FullName, temppath);
            }
        }

        private void Delete()
        {
            if (this.panels[this.activePanelIndex].isDiscs)
            {
                return;
            }

            FileSystemInfo fileObject = this.panels[this.activePanelIndex].GetActiveObject();
            try
            {
                if (fileObject is DirectoryInfo)
                {
                    ((DirectoryInfo)fileObject).Delete(true);
                }
                else
                {
                    ((FileInfo)fileObject).Delete();
                }
                this.RefreshPannels();
            }
            catch (Exception e)
            {
                this.ShowMessage(e.Message);
                return;
            }
        }

        private void CreateDirectory()
        {
            if (this.panels[this.activePanelIndex].isDiscs)
            {
                return;
            }

            string destPath = this.panels[this.activePanelIndex].Path;
            string dirName = this.AksName("Введите имя каталога: ");

            try
            {
                string dirFullName = Path.Combine(destPath, dirName);
                DirectoryInfo dir = new DirectoryInfo(dirFullName);
                if (!dir.Exists)
                {
                    dir.Create();
                }
                else
                {
                    this.ShowMessage("Каталог с таким именем уже существует");
                }
                this.RefreshPannels();
            }
            catch (Exception e)
            {
                this.ShowMessage(e.Message);
            }
        }

        private void Move()
        {
            foreach (FilePanel panel in panels)
            {
                if (panel.isDiscs)
                {
                    return;
                }
            }

            if (this.panels[0].Path == this.panels[1].Path)
            {
                return;
            }

            try
            {
                string destPath = this.activePanelIndex == 0 ? this.panels[1].Path : this.panels[0].Path;
                FileSystemInfo fileObject = this.panels[this.activePanelIndex].GetActiveObject();

                string objectName = fileObject.Name;
                string destName = Path.Combine(destPath, objectName);

                if (fileObject is FileInfo)
                {
                    ((FileInfo)fileObject).MoveTo(destName);
                }
                else
                {
                    ((DirectoryInfo)fileObject).MoveTo(destName);
                }

                this.RefreshPannels();
            }
            catch (Exception e)
            {
                this.ShowMessage(e.Message);
                return;
            }

        }

        private void Rename()
        {
            if (this.panels[this.activePanelIndex].isDiscs)
            {
                return;
            }

            FileSystemInfo fileObject = this.panels[this.activePanelIndex].GetActiveObject();
            string currentPath = this.panels[this.activePanelIndex].Path;

            string newName = this.AksName("Введите новое имя: ");
            string newFullName = Path.Combine(currentPath, newName);

            try
            {
                if (fileObject is FileInfo)
                {
                    ((FileInfo)fileObject).MoveTo(newFullName);
                }
                else
                {
                    ((DirectoryInfo)fileObject).MoveTo(newFullName);
                }
                this.RefreshPannels();
            }
            catch (Exception e)
            {
                this.ShowMessage(e.Message);
            }
        }

        private void ViewFile()
        {
            if (this.panels[this.activePanelIndex].isDiscs)
            {
                return;
            }

            FileSystemInfo fileObject = this.panels[this.activePanelIndex].GetActiveObject();
            if (fileObject is DirectoryInfo || fileObject == null)
            {
                return;
            }

            if (((FileInfo)fileObject).Length == 0)
            {
                this.ShowMessage("Файл пуст");
                return;
            }

            if (((FileInfo)fileObject).Length > 100000000)
            {
                this.ShowMessage("Файл слишком большой для просмотра");
                return;
            }

            this.DrawViewFileFrame(fileObject.Name);
            string fileContent = this.ReadFileToString(fileObject.FullName, Encoding.ASCII);

            int beginPosition = 0;
            int symbolCount = 0;
            bool endOfFile = false;
            bool beginFile = true;
            Stack<int> printSymbols = new Stack<int>();

            symbolCount = this.PrintStingFrame(fileContent, beginPosition);
            printSymbols.Push(symbolCount);
            this.PrintProgress(beginPosition + symbolCount, fileContent.Length);

            bool exit = false;
            while (!exit)
            {
                endOfFile = (beginPosition + symbolCount) >= fileContent.Length;
                beginFile = (beginPosition <= 0);

                ConsoleKeyInfo userKey = Console.ReadKey(true);
                switch (userKey.Key)
                {
                    case ConsoleKey.Escape:
                        exit = true;
                        break;
                    case ConsoleKey.PageDown:
                        if (!endOfFile)
                        {
                            beginPosition += symbolCount;
                            symbolCount = this.PrintStingFrame(fileContent, beginPosition);
                            printSymbols.Push(symbolCount);
                            this.PrintProgress(beginPosition + symbolCount, fileContent.Length);
                        }
                        break;
                    case ConsoleKey.PageUp:
                        if (!beginFile)
                        {
                            if (printSymbols.Count != 0)
                            {
                                beginPosition -= printSymbols.Pop();
                                if (beginPosition < 0)
                                {
                                    beginPosition = 0;
                                }
                            }
                            else
                            {
                                beginPosition = 0;
                            }
                            symbolCount = this.PrintStingFrame(fileContent, beginPosition);
                            this.PrintProgress(beginPosition + symbolCount, fileContent.Length);
                        }
                        break;
                }
            }

            Console.Clear();
            foreach (FilePanel fp in panels)
            {
                fp.Show();
            }
            this.ShowKeys();
        }

        private void DrawViewFileFrame(string file)
        {
            Console.Clear();
            ConsoleManager.PrintFrameDoubleLine(0, 0, Console.WindowWidth, Console.WindowHeight - 5, ConsoleColor.DarkYellow, ConsoleColor.Black);
            string fileName = String.Format(" {0} ", file);
            ConsoleManager.PrintString(fileName, (Console.WindowWidth - fileName.Length) / 2, 0, ConsoleColor.Yellow, ConsoleColor.Black);
            ConsoleManager.PrintFrameLine(0, Console.WindowHeight - 5, Console.WindowWidth, 4, ConsoleColor.DarkYellow, ConsoleColor.Black);
            ConsoleManager.PrintString("PageDown / PageUp - навигация, Esc - выход", 1, Console.WindowHeight - 4, ConsoleColor.White, ConsoleColor.Black);
        }

        private void PrintProgress(int position, int length)
        {
            string pageMessage = String.Format("Позиция: {0}%", (100 * position) / length);
            ConsoleManager.PrintString(new String(' ', Console.WindowWidth / 2 - 1), Console.WindowWidth / 2, Console.WindowHeight - 4, ConsoleColor.White, ConsoleColor.Black);
            ConsoleManager.PrintString(pageMessage, Console.WindowWidth - pageMessage.Length - 2, Console.WindowHeight - 4, ConsoleColor.White, ConsoleColor.Black);
        }

        private string ReadFileToString(string fullFileName, Encoding encoding)
        {
            StreamReader SR = new StreamReader(fullFileName, encoding);
            string fileContent = SR.ReadToEnd();
            fileContent = fileContent.Replace("\a", " ").Replace("\b", " ").Replace("\f", " ").Replace("\r", " ").Replace("\v", " ");
            SR.Close();
            return fileContent;
        }

        private int PrintStingFrame(string text, int begin)
        {
            this.ClearFileViewFrame();

            int lastTopCursorPosition = Console.WindowHeight - 7;
            int lastLeftCursorPosition = Console.WindowWidth - 2;

            Console.SetCursorPosition(1, 1);

            int currentTopPosition = Console.CursorTop;
            int currentLeftPosition = Console.CursorLeft;

            int index = begin;
            while (true)
            {
                if (index >= text.Length)
                {
                    break;
                }

                Console.Write(text[index]);
                currentTopPosition = Console.CursorTop;
                currentLeftPosition = Console.CursorLeft;

                if (currentLeftPosition == 0 || currentLeftPosition == lastLeftCursorPosition)
                {
                    Console.CursorLeft = 1;
                }

                if (currentTopPosition == lastTopCursorPosition)
                {
                    break;
                }

                index++;
            }
            return index - begin;
        }

        private void ClearFileViewFrame()
        {
            int lastTopCursorPosition = Console.WindowHeight - 7;
            int lastLeftCursorPosition = Console.WindowWidth - 2;

            for (int row = 1; row < lastTopCursorPosition; row++)
            {
                Console.SetCursorPosition(1, row);
                string space = new String(' ', lastLeftCursorPosition);
                Console.Write(space);
            }
        }

        private void RefreshPannels()
        {
            if (this.panels == null || this.panels.Count == 0)
            {
                return;
            }

            foreach (FilePanel panel in panels)
            {
                if (!panel.isDiscs)
                {
                    panel.UpdateContent(true);
                }
            }
        }

        private void ChangeActivePanel()
        {
            this.panels[this.activePanelIndex].Active = false;
            KeyPress -= this.panels[this.activePanelIndex].KeyboardProcessing;
            this.panels[this.activePanelIndex].UpdateContent(false);

            this.activePanelIndex++;
            if (this.activePanelIndex >= this.panels.Count)
            {
                this.activePanelIndex = 0;
            }

            this.panels[this.activePanelIndex].Active = true;
            KeyPress += this.panels[this.activePanelIndex].KeyboardProcessing;
            this.panels[this.activePanelIndex].UpdateContent(false);
        }

        private void ChangeDirectoryOrRunProcess()
        {
            FileSystemInfo fsInfo = this.panels[this.activePanelIndex].GetActiveObject();
            if (fsInfo != null)
            {
                if (fsInfo is DirectoryInfo)
                {
                    try
                    {
                        Directory.GetDirectories(fsInfo.FullName);
                    }
                    catch
                    {
                        return;
                    }

                    this.panels[this.activePanelIndex].Path = fsInfo.FullName;
                    this.panels[this.activePanelIndex].SetLists();
                    this.panels[this.activePanelIndex].UpdatePanel();
                }
                else
                {
                    Process.Start(((FileInfo)fsInfo).FullName);
                }
            }
            else
            {
                string currentPath = this.panels[this.activePanelIndex].Path;
                DirectoryInfo currentDirectory = new DirectoryInfo(currentPath);
                DirectoryInfo upLevelDirectory = currentDirectory.Parent;

                if (upLevelDirectory != null)
                {
                    this.panels[this.activePanelIndex].Path = upLevelDirectory.FullName;
                    this.panels[this.activePanelIndex].SetLists();
                    this.panels[this.activePanelIndex].UpdatePanel();
                }

                else
                {
                    this.panels[this.activePanelIndex].SetDiscs();
                    this.panels[this.activePanelIndex].UpdatePanel();
                }
            }
        }

        private void ShowKeys()
        {
            string[] menu = { "F3 Просмотр", " F4 Копировать", "F5 Переместить", "F6 Создать", "F7 Переименов", "F8 Удалить", "Esc Выход"};

            int cellLeft = this.panels[0].Left;
            int cellTop = FilePanel.PANEL_HEIGHT * this.panels.Count;
            int cellWidth = FilePanel.PANEL_WIDTH / menu.Length;
            int cellHeight = Explorer.HEIGHT_KEYS;

            for (int i = 0; i < menu.Length; i++)
            {
                ConsoleManager.PrintFrameLine(cellLeft + i * cellWidth, cellTop, cellWidth, cellHeight, ConsoleColor.White, ConsoleColor.Black);
                ConsoleManager.PrintString(menu[i], cellLeft + i * cellWidth + 1, cellTop + 1, ConsoleColor.White, ConsoleColor.Black);
            }
        }

        private void ShowMessage(string message)
        {
            ConsoleManager.PrintString(message, 0, Console.WindowHeight - BOTTOM_OFFSET, ConsoleColor.White, ConsoleColor.Black);
        }

        private void ClearMessage()
        {
            ConsoleManager.PrintString(new String(' ', Console.WindowWidth), 0, Console.WindowHeight - BOTTOM_OFFSET, ConsoleColor.White, ConsoleColor.Black);
        }
    }
}
