using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ARC
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ARC myArc = new ARC();
            Console.WriteLine("ARC Machine running, enter a line of assembly code ( the machine sadly only works with a single line at a time )");
            Console.WriteLine("It only supports Memory / Arithmetic formats as well"); // habar nu am cum functioneaza CALL, SETHI, si BRANCH 
            while (true)
            {
                try
                {
                    myArc.Work(Console.ReadLine());
                }
                catch (Exception e) { Console.WriteLine(e); }
            }

        }
    }
    class ARC
    {
        static List<Registry> rgs = new List<Registry>();
        static Registry rN;
        static Registry Link;
        static Registry PSR;

        static List<Memory> Mem = new List<Memory>();

        StringBuilder str { get; set; }
        public ARC()
        {
            str = new StringBuilder();
            for(int i = 0; i < 32; i++)
            {
                rgs.Add(new Registry(i));
            }
            rN = rgs[0];
            Link = rgs[15];
            PSR = new Registry(32);
            rgs.Add(PSR);
            rgs[1].SetValue(-1);
        }
        public void Work(string text)
        {
            if(text == "help")
            {
                WriteAllCommands();
                return;
            }
            text = text.Split('!')[0]; // removes comments
            string first = text.Split(' ')[0];
            

            FindCommand(first,text);

            Console.WriteLine(str.ToString());
            str.Clear();
        }

        private void WriteAllCommands()
        {
            Console.WriteLine();
            Console.WriteLine($"Commands available:{Environment.NewLine}ld {Environment.NewLine}  Example: ld %r[0-31], %r[0-31], %r[0-31] // ld %r[0-31], int, %r[0-31] , registers 14 and 15 cannot be used");
            Console.WriteLine("st, addcc, andcc, orcc, slr, printinf ");
        }
        static readonly Regex rxReg = new Regex(@"%r\d(\d?)");
        public void FindCommand(string cm, string line)
        {
            switch (cm)
            {
                case "ld": LD(line); break;
                case "st": ST(line); break;
                case "addcc": ADDCC(line); break;
                case "orncc": ORNCC(line); break;
                case "orcc": ORCC(line); break;
                case "andcc": ANDCC(line); break;
                case "srl": SRL(line); break;
                case "printinf": Info(line); break;
                default: throw new FormatException();
            }
        }
        void Info(string line) // special command to check registry values from console
        {
            MatchCollection match = rxReg.Matches(line);
            int firstreg = int.Parse(match[0].Value.Replace("%r", ""));
            if (firstreg > 31)
            {
                throw new FormatException();
            }
            rgs[firstreg].printinf();
        }
        void LD(string line)
        {
            line = line.Replace("ld ", "");
            str.Append("10");
                
            int toLoad = int.Parse(line.Split(',')[0]);

            MatchCollection match = rxReg.Matches(line);
            int firstreg = int.Parse(match[0].Value.Replace("%r", ""));
            if (firstreg > 31)
            {
                throw new FormatException();
            }
            for (int i = 0; i < 5 - Convert.ToString(firstreg, 2).Length; i++)
            {
                str.Append('0');
            }
            str.Append(Convert.ToString(firstreg, 2));

            str.Append("000000");

            str.Append("00000"); // %rs1 = %r0 ( int + 0 ) da habar nu am 

            str.Append('1');

            for (int i = 0; i < 13 - Convert.ToString(toLoad, 2).Length; i++)
            {
                str.Append('0');
            }
            str.Append(Convert.ToString(toLoad, 2));


            rgs[firstreg].SetValue(toLoad);
            
        }
        void ST(string line) // WiP memorie 
        {
            //str.Append("10");

            //int toSt = int.Parse(line.Split(' ')[1].Split(',')[0]);

            //MatchCollection match = rxReg.Matches(line);
            //int firstreg = int.Parse(match[0].Value.Replace("%r", ""));
            //if (firstreg > 31)
            //{
            //    throw new FormatException();
            //}
            //for (int i = 0; i < 5 - Convert.ToString(firstreg, 2).Length; i++)
            //{
            //    str.Append('0');
            //}
            //str.Append(Convert.ToString(firstreg, 2));

            //str.Append("000100");

            //str.Append('1');

            //for (int i = 0; i < 13 - Convert.ToString(toLoad, 2).Length; i++)
            //{
            //    str.Append('0');
            //}
            //str.Append(Convert.ToString(toLoad, 2).Length);

        }
        void ADDCC(string line)
        {
            line = line.Replace("addcc ", "");
            str.Append("11");
            MatchCollection match = rxReg.Matches(line);
            int originReg = int.Parse(match[match.Count - 1].Value.Replace("%r", ""));
            if (originReg > 31)
            {
                throw new FormatException();
            }
            for (int i = 0; i < 5 - Convert.ToString(originReg, 2).Length; i++)
            {
                str.Append('0');
            }
            str.Append(Convert.ToString(originReg, 2));
            str.Append("010000");
            if (match.Count > 2)
            {
                int firstreg = int.Parse(match[0].Value.Replace("%r", ""));
                if (firstreg > 31)
                {
                    throw new FormatException();
                }
                for (int i = 0; i < 5 - Convert.ToString(firstreg, 2).Length; i++)
                {
                    str.Append('0');
                }
                str.Append(Convert.ToString(firstreg, 2));
                int secondreg = int.Parse(match[1].Value.Replace("%r", ""));
                if (secondreg > 31)
                {
                    throw new FormatException();
                }
                    
                str.Append('0');

                str.Append("00000000");
                for (int i = 0; i < 5 - Convert.ToString(secondreg, 2).Length; i++)
                {
                    str.Append('0');
                }
                str.Append(Convert.ToString(secondreg, 2));

                rgs[originReg].SetValue(rgs[firstreg].Value() + rgs[secondreg].Value());
            }
            else
            {
                int firstreg = int.Parse(match[0].Value.Replace("%r", ""));
                if (firstreg > 31)
                {
                    throw new FormatException();
                }
                for (int i = 0; i < 5 - Convert.ToString(firstreg, 2).Length; i++)
                {
                    str.Append('0');
                }
                str.Append(Convert.ToString(firstreg, 2));
                int toAdd = 0;
                for (int i = 0; i < 3; i++)
                {
                    if (int.TryParse(line.Replace(" ", "").Split(',')[i], out toAdd))
                    {
                        break;
                    }
                }

                str.Append('1');

                string stradd = Convert.ToString(toAdd, 2);
                for(int i = 0; i < 13 - stradd.Length; i++)
                {
                    str.Append('0');
                }
                str.Append(stradd);
                rgs[originReg].SetValue(rgs[firstreg].Value() + toAdd);
            }
        }
        void ORCC(string line)
        {
            line = line.Replace("orcc ", "");

            str.Append("11");
            MatchCollection match = rxReg.Matches(line);
            int originReg = int.Parse(match[match.Count - 1].Value.Replace("%r", ""));
            if (originReg > 31)
            {
                throw new FormatException();
            }
            for (int i = 0; i < 5 - Convert.ToString(originReg, 2).Length; i++)
            {
                str.Append('0');
            }
            str.Append(Convert.ToString(originReg, 2));
            str.Append("010010"); // op
            if (match.Count > 2)
            {
                int firstreg = int.Parse(match[0].Value.Replace("%r", ""));
                if (firstreg > 31)
                {
                    throw new FormatException();
                }
                for (int i = 0; i < 5 - Convert.ToString(firstreg, 2).Length; i++)
                {
                    str.Append('0');
                }
                str.Append(Convert.ToString(firstreg, 2));
                int secondreg = int.Parse(match[1].Value.Replace("%r", ""));
                if (secondreg > 31)
                {
                    throw new FormatException();
                }

                str.Append('0');

                str.Append("00000000");
                for (int i = 0; i < 5 - Convert.ToString(secondreg, 2).Length; i++)
                {
                    str.Append('0');
                }
                str.Append(Convert.ToString(secondreg, 2));

                rgs[originReg].SetValue(rgs[firstreg].Value() | rgs[secondreg].Value());
            }
            else
            {
                int firstreg = int.Parse(match[0].Value.Replace("%r", ""));
                if (firstreg > 31)
                {
                    throw new FormatException();
                }
                for (int i = 0; i < 5 - Convert.ToString(firstreg, 2).Length; i++)
                {
                    str.Append('0');
                }
                str.Append(Convert.ToString(firstreg, 2));
                int toAdd = 0;
                for (int i = 0; i < 3; i++)
                {
                    if (int.TryParse(line.Replace(" ", "").Split(',')[i], out toAdd))
                    {
                        break;
                    }
                }

                str.Append('1');

                string stradd = Convert.ToString(toAdd, 2);
                for (int i = 0; i < 13 - stradd.Length; i++)
                {
                    str.Append('0');
                }
                str.Append(stradd);
                rgs[originReg].SetValue(rgs[firstreg].Value() | toAdd);
            }
        }
        void ORNCC(string line)
        {
            line = line.Replace("orncc ", "");
            str.Append("11");
            MatchCollection match = rxReg.Matches(line);
            int originReg = int.Parse(match[match.Count - 1].Value.Replace("%r", ""));
            if (originReg > 31)
            {
                throw new FormatException();
            }
            for (int i = 0; i < 5 - Convert.ToString(originReg, 2).Length; i++)
            {
                str.Append('0');
            }
            str.Append(Convert.ToString(originReg, 2));
            str.Append("010001"); // op
            if (match.Count > 2)
            {
                int firstreg = int.Parse(match[0].Value.Replace("%r", ""));
                if (firstreg > 31)
                {
                    throw new FormatException();
                }
                for (int i = 0; i < 5 - Convert.ToString(firstreg, 2).Length; i++)
                {
                    str.Append('0');
                }
                str.Append(Convert.ToString(firstreg, 2));
                int secondreg = int.Parse(match[1].Value.Replace("%r", ""));
                if (secondreg > 31)
                {
                    throw new FormatException();
                }

                str.Append('0');

                str.Append("00000000");
                for (int i = 0; i < 5 - Convert.ToString(secondreg, 2).Length; i++)
                {
                    str.Append('0');
                }
                str.Append(Convert.ToString(secondreg, 2));

                // rgs[originReg].SetValue((rgs[firstreg].Value() | rgs[secondreg].Value())); NOR W I P
            }
            else
            {
                int firstreg = int.Parse(match[0].Value.Replace("%r", ""));
                if (firstreg > 31)
                {
                    throw new FormatException();
                }
                for (int i = 0; i < 5 - Convert.ToString(firstreg, 2).Length; i++)
                {
                    str.Append('0');
                }
                str.Append(Convert.ToString(firstreg, 2));
                int toAdd = 0;
                for (int i = 0; i < 3; i++)
                {
                    if (int.TryParse(line.Replace(" ", "").Split(',')[i], out toAdd))
                    {
                        break;
                    }
                }

                str.Append('1');

                string stradd = Convert.ToString(toAdd, 2);
                for (int i = 0; i < 13 - stradd.Length; i++)
                {
                    str.Append('0');
                }
                str.Append(stradd);
                // rgs[originReg].SetValue(rgs[firstreg].Value() & toAdd); NOR W I P
            }
        }
        void ANDCC(string line)
        {
            line = line.Replace("andcc ", "");
            str.Append("11");
            MatchCollection match = rxReg.Matches(line);
            int originReg = int.Parse(match[match.Count - 1].Value.Replace("%r", ""));
            if (originReg > 31)
            {
                throw new FormatException();
            }
            for (int i = 0; i < 5 - Convert.ToString(originReg, 2).Length; i++)
            {
                str.Append('0');
            }
            str.Append(Convert.ToString(originReg, 2));
            str.Append("010001"); // op
            if (match.Count > 2)
            {
                int firstreg = int.Parse(match[0].Value.Replace("%r", ""));
                if (firstreg > 31)
                {
                    throw new FormatException();
                }
                for (int i = 0; i < 5 - Convert.ToString(firstreg, 2).Length; i++)
                {
                    str.Append('0');
                }
                str.Append(Convert.ToString(firstreg, 2));
                int secondreg = int.Parse(match[1].Value.Replace("%r", ""));
                if (secondreg > 31)
                {
                    throw new FormatException();
                }

                str.Append('0');

                str.Append("00000000");
                for (int i = 0; i < 5 - Convert.ToString(secondreg, 2).Length; i++)
                {
                    str.Append('0');
                }
                str.Append(Convert.ToString(secondreg, 2));

                rgs[originReg].SetValue(rgs[firstreg].Value() & rgs[secondreg].Value());
            }
            else
            {
                int firstreg = int.Parse(match[0].Value.Replace("%r", ""));
                if (firstreg > 31)
                {
                    throw new FormatException();
                }
                for (int i = 0; i < 5 - Convert.ToString(firstreg, 2).Length; i++)
                {
                    str.Append('0');
                }
                str.Append(Convert.ToString(firstreg, 2));
                int toAdd = 0;
                for (int i = 0; i < 3; i++)
                {
                    if (int.TryParse(line.Replace(" ", "").Split(',')[i], out toAdd))
                    {
                        break;
                    }
                }

                str.Append('1');

                string stradd = Convert.ToString(toAdd, 2);
                for (int i = 0; i < 13 - stradd.Length; i++)
                {
                    str.Append('0');
                }
                str.Append(stradd);
                rgs[originReg].SetValue(rgs[firstreg].Value() & toAdd);
            }
        }
        void SRL(string line)
        {
            line = line.Replace("srl ", "");
            str.Append("11");
            MatchCollection match = rxReg.Matches(line);
            int originReg = int.Parse(match[match.Count - 1].Value.Replace("%r", ""));
            if (originReg > 31)
            {
                throw new FormatException();
            }
            for (int i = 0; i < 5 - Convert.ToString(originReg, 2).Length; i++)
            {
                str.Append('0');
            }
            str.Append(Convert.ToString(originReg, 2));
            str.Append("100110"); // op
            int firstreg = int.Parse(match[0].Value.Replace("%r", ""));
            if (firstreg > 31)
            {
                throw new FormatException();
            }
            for (int i = 0; i < 5 - Convert.ToString(firstreg, 2).Length; i++)
            {
                str.Append('0');
            }
            str.Append(Convert.ToString(firstreg, 2));
            int toAdd = 0;
            for (int i = 0; i < 3; i++)
            {
                if (int.TryParse(line.Replace(" ", "").Split(',')[i], out toAdd))
                {
                    break;
                }
            }
            str.Append('1');

            string stradd = Convert.ToString(toAdd, 2);
            for (int i = 0; i < 13 - stradd.Length; i++)
            {
                str.Append('0');
            }
            str.Append(stradd);
            rgs[originReg].SetValue(rgs[firstreg].Value() << toAdd);
        }       
    }
    class Memory // wip
    {
        public int Value { get; set; }
        public string Identifier { get; set; }
        public Memory(int value, string identifier)
        {
            Value = value; Identifier = identifier;
        }
    }
    class Registry
    {
        int value { get; set; }
        char[] bits = new char[32];
        bool isNull = false;
        bool isSp = false;
        bool isLink = false;
        public Registry(int i)
        {
            switch (i)
            {
                case 0: isNull= true; break;
                case 14: isSp= true; break;
                case 15: isLink= true; break;
                default: break;
            }
            value = 0;
            for (int j = 0; j < bits.Length; j++)
            {
                bits[j] = '0';
            }
        }
        public int Value() { return value; }
        public bool IsNull() { return isNull; }
        public bool IsSp() { return isSp; }
        public bool IsLink() { return isLink; }
        public char[] Binary() { return bits; }

        public void printinf()
        {
            Console.WriteLine($"This registry holds the value of {value} stored as: ");
            for(int i = 0; i < bits.Length; i++)
            {
                Console.Write(bits[i]);
            }
            Console.WriteLine();
            if (isNull) { Console.WriteLine("This registry is %r0."); }
            if (isSp) { Console.WriteLine("This registry is the Sp registry"); }
            if (isLink) { Console.WriteLine("This registry is the Link registry"); }
        }
        public void SetValue(int value)
        {
            this.value = value;
            string b = Convert.ToString(value, 2);
            for(int i = b.Length - 1, j = bits.Length - 1; i >= 0; i--, j--)
            {
                bits[j] = b[i];
            }
        }
    }


    
}
