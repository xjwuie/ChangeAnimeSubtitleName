using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;

namespace ChangeSubtitleName
{
    class Program
    {
        static void Main(string[] args) {
            int argNum = args.Length;
            string dir;
            
            if(argNum == 0)
            {
                Console.WriteLine("please input the directory");
                dir = Console.ReadLine();
                while (!Directory.Exists(dir)){
                    Console.WriteLine("please input a valid directory");
                    dir = Console.ReadLine();
                }
                //dir = @"H:\影音\[SweetSub&VCB-Studio] DARLING in the FRANXX [Ma10p_1080p]";
                //dir = @"H:\影音\[2018][Violet Evergarden][BDRIP][1080P][1-13Fin+Extra+Movie+SP]";
                //dir = @"H:\影音\Saiki Kusuo no Sainan [Ma444-10p_1080p]";
            }
            else
            {
                dir = args[0];
            }

            SubtitleHandler handler = new SubtitleHandler(dir);
            handler.Test();

            Console.WriteLine("Continue? y/n");
            string str = Console.ReadLine().ToString().ToLower();
            if (str == "y" || str == "yes")
            {
                Console.WriteLine("go on");
                handler.Change();
            }
                
            
        }
    }
}
