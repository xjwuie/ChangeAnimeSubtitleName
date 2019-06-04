using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace ChangeSubtitleName
{
    class CustomFileInfo
    {
        public bool isNormal;

        public FileInfo fileInfo;
        public int index;
        public string fileName;
        public string lowerName;
        public string extName;
        public string newName;
        public bool completed = false;

        public CustomFileInfo(FileInfo info) {
            fileInfo = info;
            fileName = info.Name;
            //fileName = fileName.Substring(0, fileName.IndexOf("."));
            fileName = fileName.Substring(0, fileName.Length - info.Extension.Length);
            lowerName = fileName.ToLower();
            //extName = info.Name.Substring(info.Name.IndexOf("."));
            extName = info.Name.Substring(fileName.Length);
        }
    }

    class SubtitleHandler
    {
        static string[] subExtSet = { "sub", "ass", "srt" };
        static string[] videoExtSet = { "mkv", "mp4" };

        string folderName;
        string subExt, videoExt;
        FileInfo[] files;
        List<CustomFileInfo> subFiles, videoFiles, unknownSubFiles, allVideoFiles, tmpVideoFiles;
        List<string> oldSubNames, newSubNames;
        int subNum = 0, normalCount = 0, ovaCount = 0;

        public SubtitleHandler(string dir) {
            folderName = dir;
            DirectoryInfo dirInfo = new DirectoryInfo(dir);
            files = dirInfo.GetFiles();
        }

        public void Test() {
            if (!SetFileExt())
            {
                Console.WriteLine("extension err");
            }
            GetSubFiles();
            GetSubIndex();
            Console.WriteLine("sub num: " + subNum.ToString());
            GetVideoFiles();
            Console.WriteLine("video total num: " + allVideoFiles.Count);
            if (VideoFilesFilter())
            {
                PrepareNames();
            }

        }

        public void Change() {
            foreach(CustomFileInfo file in subFiles)
            {
                Console.WriteLine(file.newName);
                file.fileInfo.MoveTo(folderName + @"\" + file.newName);
            }
            Console.WriteLine("Completed");
        }


        bool SetFileExt() {
            foreach(FileInfo file in files)
            {
                string ext = file.Extension.Substring(1);
                //Console.WriteLine(ext);
                if(subExt == null)
                {
                    int index = Array.IndexOf(subExtSet, ext);
                    if (index != -1)
                    {
                        subExt = subExtSet[index];
                        if (videoExt != null)
                            return true;
                    }
                }
                
                if(videoExt == null)
                {
                    int index = Array.IndexOf(videoExtSet, ext);
                    if (index != -1)
                    {
                        videoExt = videoExtSet[index];
                        if (subExt != null)
                            return true;
                    }
                }
                
            }
            return false;
        }

        void GetSubFiles() {
            oldSubNames = new List<string>();
            subFiles = new List<CustomFileInfo>();
            
            foreach(FileInfo file in files)
            {
                if(file.Extension.Substring(1) == subExt)
                {
                    oldSubNames.Add(file.Name);
                    subFiles.Add(new CustomFileInfo(file));
                    //Console.WriteLine(file.Name);
                }
            }
            oldSubNames.Sort(Utils.CompareString);
            subFiles.Sort(Utils.CompareFileByName);

        }

        void GetVideoFiles() {
            videoFiles = new List<CustomFileInfo>();
            allVideoFiles = new List<CustomFileInfo>();
            foreach(FileInfo file in files)
            {
                if(file.Extension.Substring(1) == videoExt)
                {
                    allVideoFiles.Add(new CustomFileInfo(file));
                }
            }
            allVideoFiles.Sort(Utils.CompareFileByName);
        }

        bool GetSubIndex() {

            unknownSubFiles = new List<CustomFileInfo>();

            int index = 0, cutLength = 0;
            CustomFileInfo file0 = subFiles[0];
            string fileName = file0.lowerName;
            //Console.WriteLine(fileName);
            int num = fileName.Length;
            Regex regex;
            Match match;

            int tmpI = 0;
            while (tmpI++ < 5)
            {
                regex = new Regex(@"(((ova|sp|extra|oad)(\s*))?(\d+))|(ova|sp|extra|oad)");
                match = regex.Match(fileName);
                
                string tmp = match.Groups[0].ToString();
                Console.WriteLine(tmp);
                if (tmp.Length == 0)
                {
                    return false;
                }     
                
                index = file0.lowerName.IndexOf(tmp);
                if (CheckSubPattern(index))
                {
                    //Console.WriteLine("break");
                    break;
                }
                cutLength = index + tmp.Length;
                fileName = file0.lowerName.Substring(cutLength);
            }

            if (fileName.Length == 0)
                return false;
            List<string> counter = new List<string>();
            
            foreach(CustomFileInfo file in subFiles)
            {
                fileName = file.lowerName.Substring(index);
                //Console.WriteLine(fileName);
                regex = new Regex(@"(((ova|sp|extra|oad)(\s*))?(\d+))|(ova|sp|extra|oad)");
                match = regex.Match(fileName);
                

                string matchStr, numStr, frontStr;
                matchStr = match.Groups[0].ToString();
                if (matchStr == "" || file.lowerName.IndexOf(matchStr) != index)
                {
                    unknownSubFiles.Add(file);
                    //Console.WriteLine
                    //Console.WriteLine(matchStr);
                    Console.WriteLine("unknown: " + file.fileName);
                    continue;
                }
                numStr = match.Groups[5].ToString();
                frontStr = match.Groups[6].ToString() == "" ? match.Groups[3].ToString() : match.Groups[6].ToString();
                //Console.WriteLine(matchStr);
                if(frontStr == "")
                {
                    file.isNormal = true;
                    if (!counter.Contains(matchStr))
                    {
                        counter.Add(matchStr);
                        file.index = normalCount++;
                        file.completed = true;
                    }
                    else
                    {
                        file.index = normalCount - 1;
                        file.completed = true;
                    }
                }
                else
                {
                    file.isNormal = false;
                    if(numStr == "")
                    {
                        ovaCount = 1;
                        file.index = 0;
                        file.completed = true;
                    }
                    else
                    {
                        if (!counter.Contains(matchStr))
                        {
                            counter.Add(matchStr);
                            file.index = ovaCount++;
                            file.completed = true;
                        }
                        else
                        {
                            file.index = ovaCount - 1;
                            file.completed = true;
                        }
                    }
                }
            }
            //foreach (CustomFileInfo file in subFiles) Console.WriteLine(file.index);
            //foreach(Group g in match.Groups) Console.WriteLine("***" + g + "***");
            subNum = normalCount + ovaCount;
            if (subNum == 0)
                return false;
            return true;
        }

        bool CheckSubPattern(int index) {
            int count = 0, i = 0, num = subFiles.Count;
            char ch = subFiles[i].lowerName[index];
            while(i < num)
            {
                if (ch == subFiles[i++].lowerName[index])
                {
                    count++;
                }
                
            }
            //Console.WriteLine(count);
            if (count == num)
            {
                return false;
            }
            return true;
        }

        bool VideoFilesFilter() {
            if(allVideoFiles.Count == subNum)
            {
                foreach(CustomFileInfo file in allVideoFiles)
                {
                    videoFiles.Add(file);
                    //Console.WriteLine(file.fileName);
                }
                return true;
            }

            tmpVideoFiles = new List<CustomFileInfo>();
            for(int i = 0; i <= allVideoFiles.Count - subNum; ++i)
            {
                tmpVideoFiles.Clear();
                tmpVideoFiles.Add(allVideoFiles[i]);
                long baseSize = allVideoFiles[i].fileInfo.Length;
                long minSize = baseSize / 2, maxSize = baseSize * 2;
                for(int j = i+1; j < allVideoFiles.Count; ++j)
                {
                    if (minSize < allVideoFiles[j].fileInfo.Length && allVideoFiles[j].fileInfo.Length < maxSize)
                    {
                        tmpVideoFiles.Add(allVideoFiles[j]);
                    }
                }

                if(tmpVideoFiles.Count >= subNum)
                {
                    if (CheckVideoFiles())
                    {
                        //Console.WriteLine("after check func");
                        //foreach (CustomFileInfo file in videoFiles) Console.WriteLine(file.fileName);
                        return true;
                    }
                }
            }

            return false;
        }

        bool CheckVideoFiles() {
            if(tmpVideoFiles.Count == subNum)
            {
                foreach(CustomFileInfo file in tmpVideoFiles)
                {
                    videoFiles.Add(file);
                }
                return true;
            }

            foreach(CustomFileInfo file in tmpVideoFiles)
            {
                bool found = false;
                string fileName = file.lowerName;
                int num = fileName.Length, index = 0;
                while(index < num)
                {
                    //bool flag = false;
                    Regex reg = new Regex(@"(((ova|sp|extra|oad)(\s*))?(\d+))|(ova|sp|extra|oad)");
                    Match match = reg.Match(fileName);
                    string matchStr = match.Groups[0].ToString();
                    if(matchStr == "")
                    {
                        break;
                    }
                    index = fileName.IndexOf(matchStr);
                    int count = 0;
                    char ch = matchStr[0];
                    foreach(CustomFileInfo f in tmpVideoFiles)
                    {
                        if (f.lowerName[index] == ch)
                            count++;
                    }
                    if(count <= tmpVideoFiles.Count / 2 + 1)
                    {
                        found = true;
                        break;
                    }
                    else
                    {
                        fileName = fileName.Substring(index + matchStr.Length);
                    }
                }

                if (found)
                {
                    foreach(CustomFileInfo f in tmpVideoFiles)
                    {
                        Regex reg = new Regex(@"(((ova|sp|extra|oad)(\s*))?(\d+))|(ova|sp|extra|oad)");
                        Match match = reg.Match(f.lowerName);
                        string matchStr = match.Groups[0].ToString();
                        if (matchStr == "")
                            continue;
                        else
                        {
                            if(f.lowerName.IndexOf(matchStr) == index)
                            {
                                videoFiles.Add(f);
                            }
                        }
                    }
                    if (videoFiles.Count == subNum)
                        return true;
                    else
                    {
                        return false;
                    }
                }
            }
            return false;

        }


        void PrepareNames() {
            newSubNames = new List<string>();
            Console.WriteLine("subFiles num: {0}", subFiles.Count);
            foreach(CustomFileInfo file in subFiles)
            {
                if (!file.isNormal)
                {
                    file.index = file.index + normalCount;
                }
                int index = file.index;
                CustomFileInfo video = videoFiles[index];
                string newName = video.fileName;
                file.newName = newName + file.extName;

                newSubNames.Add(file.newName);
            }

            string pad = "\t";
            for(int i = 0; i < oldSubNames.Count; i++)
            {
                if (pad == "\t")
                    pad = "";
                else
                    pad = "\t";
                Console.WriteLine(pad + oldSubNames[i]);
                Console.WriteLine(pad + "-->\t");
                Console.WriteLine(pad + newSubNames[i]);
                Console.WriteLine();
            }
        }


    }

    static class Utils
    {
        public static int CompareFileByName(CustomFileInfo f1, CustomFileInfo f2) {
            return f1.fileInfo.Name.CompareTo(f2.fileInfo.Name);
        }

        public static int CompareString(string s1, string s2) {
            return s1.CompareTo(s2);
        }
    }
}
