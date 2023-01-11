using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using static System.Net.Mime.MediaTypeNames;

namespace ARC
{
    internal class Program
    {
        readonly static string path = "rawcode.arc";
        
        static void Main(string[] args)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            File.Create(path).Dispose();
            ARC myArc = new ARC();
            Console.WriteLine("ARC Machine running, machine will execute code from start, to change entry point start code with call functionname");
            Console.WriteLine("Compiling will not happen until the user inputs the .end command after which the machine will spit out all instructions as a 32bit sequence for each line executed.");
            Console.WriteLine("To enter debug mode write .debug (used to check what the machine actually did after compiling code)");
            Console.WriteLine("Type '.help' for available commands and their functionalities and more details");
            Console.WriteLine("! Compilation errors will reset ARC CPU registries, memory should be unaffected (emphasis on should)"); // wip CALL / BRANCH , sethi is automatically handled 
            Console.WriteLine(".begin             ! your code will start from here");
            while (true)
            {
                try
                {
                    string toCheck = Console.ReadLine();
                    LineCheck(toCheck, myArc);
                }
                catch (Exception e) 
                { 
                    Console.WriteLine($"{e.Message}");
                    myArc.Reset();
                }
            }

        }
        static void LineCheck(string line, ARC arc)
        {
            if (line.Replace(" ","") == ".debug")
            {
                arc.Debug();
            }
            if (line.Replace(" ","") == ".help")
            {
                arc.WriteAllCommands();
            }
            if (line.Replace(" ", "") == ".reset")
            {
                arc.Reset();
                Console.WriteLine(".begin");
            }
            if (line.Replace(" ","") == ".end")
            {
                arc.Execute();
            }
            else
            {
                using (StreamWriter sw = new StreamWriter(path, true))
                {
                    sw.WriteLine(line.Split('!')[0]); // removes comments;
                }
            }
        }
    }
    class ARC
    {
        readonly static string path = "rawcode.arc";

        List<Registry> rgs = new List<Registry>();
        Registry rN;
        Registry sp;
        Registry Link;
        PSR State { get; set; }

        Registry PC;

        List<Memory> Mem = new List<Memory>();
        List<Memory> Func = new List<Memory>();

        StringBuilder str { get; set; }
        public ARC()
        {
            State = new PSR();
            str = new StringBuilder();
            for(int i = 0; i < 32; i++)
            {
                rgs.Add(new Registry(i));
            }
            rN = rgs[0];
            sp = rgs[14];
            Link = rgs[15];
            PC = new Registry(32);
            rgs.Add(PC);
            Func.Add(new Memory(0, "BaseFunc", true));
        }
        void LoadMem(string line,int lineval)
        {
            if (line.Contains(":"))
            {
                if (int.TryParse(line.Split(':')[1], out int value))
                {
                    Mem.Add(new Memory(value, line.Split(':')[0], false));
                    Func.Add(new Memory(lineval, line.Split(':')[0], false));
                }
                else
                {
                    
                    Func.Add(new Memory(lineval, line.Split(':')[0], true));
                }
            }
            else return;
        }
        public void Execute()
        {
            int i = 0;
            using (StreamReader sr = new StreamReader(path))
            {
                while (!sr.EndOfStream)
                {
                    LoadMem(sr.ReadLine(), i);
                    i++;
                }
            }
             //i--;
            int helper = 0;
            Stack<int> ParentFunc = new Stack<int>();
            while (Link.Value() < Func.Count)
            {
                try
                {
                    int linkhelper = Link.Value();
                    int nextfuncpos;
                    if(Link.Value() == Func.Count - 1)
                    {
                        nextfuncpos = i;
                    }
                    else nextfuncpos = Func[Link.Value() + 1].Value;

                    while (helper < nextfuncpos && PC.Value() == helper)
                    {
                        PC.SetValue(PC.Value() + 1);
                        Work(GetLine(path, helper));
                        helper++;
                    }
                checkcall: if (Link.Value() != linkhelper)
                    {
                        //if (State.J())
                        //{
                        //    goto jump;
                        //}
                        ParentFunc.Push(helper);

                        linkhelper = Link.Value();
                        if (Link.Value() == Func.Count)
                        {
                            break;
                        }
                        else if (Link.Value() == Func.Count - 1)
                        {
                            nextfuncpos = i;
                        }
                        else
                        {
                            nextfuncpos = Func[Link.Value() + 1].Value;
                        }
                        helper = Func[Link.Value()].Value;
                        PC.SetValue(helper);
                        ExecCall(ref helper,ref nextfuncpos);
                        goto checkcall;
                    }
                    else
                    {
                        if (ParentFunc.Count > 0)
                        {
                            int checker = ParentFunc.Pop();
                            helper = checker;
                            bool found = false;
                            for (int j = 0; j < Func.Count; j++)
                            {
                                if (Func[j].Value > checker)
                                {
                                    found = true;
                                    Link.SetValue(j - 1);
                                    PC.SetValue(helper);
                                    nextfuncpos = Func[j].Value;
                                    ExecCall(ref helper, ref nextfuncpos);
                                    break;
                                }
                            }
                            if (!found)
                            {
                                Link.SetValue(Func.Count - 1);
                                PC.SetValue(helper);
                                nextfuncpos = i;
                                ExecCall(ref helper, ref nextfuncpos);
                            }
                        }
                        else
                        {
                            Link.SetValue(Link.Value() + 1);
                        }
                    }
                    
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Reset();
                    File.Delete(path);
                    File.Create(path).Dispose();
                    Console.WriteLine("You can try again:");
                    Console.WriteLine(".begin");
                }               
            }
            Console.WriteLine("Looks like it compiled and executed your code, you can try another code using the .reset command or debug the current code with .debug:");
            Console.WriteLine(".begin");
        }
        void ExecCall(ref int helper,ref int nextfuncpos)
        {
            while (helper < nextfuncpos && PC.Value() == helper)
            {
                PC.SetValue(PC.Value() + 1);
                Work(GetLine(path, helper));
                helper++;
            }
            if(PC.Value() != helper)
            {
                helper = PC.Value();
            }
        }
        public void Reset()
        {
            foreach(Registry r in rgs)
            {
                r.SetValue(0);
            }
            State.Reset();
            str.Clear();
            Mem = new List<Memory>();
            Func = new List<Memory>();
            Console.WriteLine("~machine was reset to a stable state~");
        }
        public void Work(string text)
        {        
            
            if (text.Contains(':'))  // doesn't need labels, only executed lines of command
            {
                text = text.Split(':')[1];
                text = text.Trim();
            }
            char[] sep = new char[] { ' ' };
            string first = text.Split(sep,StringSplitOptions.RemoveEmptyEntries)[0];

            FindCommand(first,text);

            Console.WriteLine(str.ToString());
            str.Clear();
        }
        public void Debug()
        {
            Console.WriteLine("~~MACHINE IN DEBUG MODE~~");
            Console.WriteLine("you can only use the commands 'printinf' and 'meminf' in this mode, it will show information about registry, and memory");
            Console.WriteLine("  Example: printinf %r4 // will show information about what %r4 contains (does not actually do anything within the machine it's just to check if operations were executed).");
            Console.WriteLine("  Example: meminf [MEMID] // shows info about memory fragment");
            Console.WriteLine("to end debug mode write '.enddebug'");
            string s = Console.ReadLine();
            while(s != ".enddebug")
            {
                if(s.Split(' ')[0] == "printinf")
                {
                    Info(s);
                }
                if (s.Split(' ')[0] == "meminf")
                {
                    foreach (Memory m in Mem)
                    {
                        Console.WriteLine(m.ToString());
                    }
                }
                else Console.WriteLine("invalid debug command syntax");
                s = Console.ReadLine();
            }
            Console.WriteLine("~~MACHINE IN NORMAL MODE~~");
        }
        public void WriteAllCommands()
        {
            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
            Console.WriteLine("ARC Help page: ( this doesn't get compiled, just like the .debug menu");
            Console.WriteLine($"Commands available:{Environment.NewLine}'ld' {Environment.NewLine}  Example: ld 5, %r25 // loads value 5 into registry 25, there are 31 of them and 14 and 15 cannot be used either");
            Console.WriteLine("  Example with memory ( if there is one ) : ld [memID], %r25");
            Console.WriteLine("'st'");
            Console.WriteLine("  Example: st %r5, [memID] // stores value of registry 5 into a memory identified by it's id, id has to be between []");
            Console.WriteLine("'addcc'");
            Console.WriteLine("  Example: addcc %r5, 5, %r6   // adds 5 to registry 5 value and stores it in registry 6");
            Console.WriteLine("  Example: addcc %r5, %r2, %r6   // adds %r5 value to registry 2 value and stores it in registry 6");
            Console.WriteLine("'andcc / orcc / orncc'");
            Console.WriteLine("  Example: andcc %r5, 5, %r6   // bitwise operator & , stores result in registry 6, same rules apply as with addition, orncc might not work");
            Console.WriteLine("'srl'");
            Console.WriteLine("  Example: srl %r5, int x, %r22 // shift right logical operator, shifts the registry 5 value bitwise to the right x times and stores it in registry 22");    
            Console.WriteLine("'printinf'");
            Console.WriteLine("  Example: printinf %r4 // will show information about what %r4 contains (does not actually do anything within the machine it's just to check if operations were executed).");

            Console.WriteLine("~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~");
        
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
                case "jumpl": JUMPL(line); break;
                case "call": CALL(line); break;
                default: throw new FormatException("Invalid assembly command");
            }
            if(State.OpNull)  // it means output registry was r0 so whatever got stored there is overwritten by 0 as soon as command is executed
            {
                rgs[0].SetValue(0);
                State.OpNull = false;
            }
        }
        void Info(string line) // special command to check registry values from console
        {
            MatchCollection match = rxReg.Matches(line);
            if(match.Count == 0)
            {
                if (line.Split(' ')[1].ToLower() == "%psr")
                {
                    Console.WriteLine($"Processor State: N: {State.N()} ; Z: {State.Z()} ; V: {State.V()} ; C: {State.C()}");
                }
                else Console.WriteLine("Invalid registry id");
                return;
            }
            int firstreg = int.Parse(match[0].Value.Replace("%r", ""));
            if (firstreg > 31)
            {
                Console.WriteLine("registry id out of bounds");
                return;
            }
            rgs[firstreg].printinf();
        }
        void LD(string line)
        {
            line = line.Replace("ld ", "");
            str.Append("11");
            int toLoad = 0;

            int firstreg = FindReg(line, 0);

            str.Append("000000"); // op

            str.Append("00000"); // %rs1 = %r0 ( int + 0 ) (?) 

            if (line.Contains("["))
            {
                str.Append('0');
                string memID = line.Split(']')[0].Split('[')[1];
                int i = 0;
                int memlocation = -1;
                foreach (Memory m in Mem)
                {
                    if (m.Identifier == memID)
                    {
                        memlocation = i;
                        toLoad = m.Value;
                        break;
                    }
                    i++;
                }
                if(memlocation == -1)
                {
                    throw new Exception("this part of the memory is not initialized!!!");
                }
                sp.SetValue(memlocation);
                str.Append("00000000");
                str.Append("01110"); // %rs2 will be %sp that shows where memory fragment we are loading is located

                rgs[firstreg].SetValue(toLoad);
                return;
            }
            else
            {
                str.Append('1');
                toLoad = FindNumber(line);
            }
            rgs[firstreg].SetValue(toLoad);

            // PSR 
            State.ResV();
            State.SetC();
            if (rgs[firstreg].Value() < 0)
            {
                State.SetN();
            }
            else State.ResN();
            if (rgs[firstreg].Value() == 0 || firstreg == 0)
            {
                State.SetZ();
            }
            else State.ResZ();

        }
        void ST(string line) // possibly bad output, i use %sp as target registry (rd) , rs1 is %r0 and rs2 is source 
        {
            str.Append("11");

            string toSt = line.Split('[')[1].Split(']')[0];
            Memory Store = new Memory(0, "null", false);
            int Location = Mem.Count;
            bool found = false;
            int i = 0;
            foreach (Memory m in Mem)
            {
                if (m.Identifier == toSt)
                {
                    Location = i;
                    Store = m;
                    found = true;
                    break;
                }
                i++;
            }
            if (!found)
            {
                Store = new Memory(0, toSt, false);
            }

            sp.SetValue(Location);

            str.Append("01110"); // %sp

            str.Append("000100"); // op

            str.Append("00000"); // %r0

            str.Append('0');

            str.Append("00000000");

            int firstreg = FindReg(line, 0);

            Store.Value = rgs[firstreg].Value();

            Mem.Add(Store);

            //PSR values
            State.ResV();
            State.SetC();
            if (rgs[firstreg].Value() < 0)
            {
                State.SetN();
            }
            else State.ResN();
            if (rgs[firstreg].Value() == 0 || firstreg == 0)
            {
                State.SetZ();
            }
            else State.ResZ();

        }
        void ADDCC(string line)
        {
            line = line.Replace("addcc ", "");
            str.Append("10");
            MatchCollection match = rxReg.Matches(line);
            int originReg = FindReg(line, 3);
            str.Append("010000");
            if (match.Count > 2)
            {
                int firstreg = FindReg(line, 0);

                str.Append('0');

                str.Append("00000000");

                int secondreg = FindReg(line, 1);
                
                checked
                {
                    try
                    {
                        rgs[originReg].SetValue(rgs[firstreg].Value() + rgs[secondreg].Value());
                        State.ResV();
                    }
                    catch (OverflowException)
                    {
                        rgs[originReg].SetValue(rgs[firstreg].Value() + rgs[secondreg].Value());
                        State.SetV();
                    }
                }
            }
            else
            {
                int firstreg = FindReg(line, 0);

                str.Append('1');

                int toAdd = FindNumber(line);

                checked
                {
                    try
                    {
                        rgs[originReg].SetValue(rgs[firstreg].Value() + toAdd);
                        State.ResV();
                    }
                    catch (OverflowException)
                    {
                        rgs[originReg].SetValue(rgs[firstreg].Value() + toAdd);
                        State.SetV();
                    }
                }
            }
            State.ResC();
            if (rgs[originReg].Value() < 0)
            {
                State.SetN();
            }
            else State.ResN();
            if (rgs[originReg].Value() == 0 || originReg == 0)
            {
                State.SetZ();
            }
            else State.ResZ();

        }
        void ORCC(string line)
        {
            line = line.Replace("orcc ", "");

            str.Append("10");
            MatchCollection match = rxReg.Matches(line);
            int originReg = FindReg(line, 3);
            str.Append("010010"); // op
            if (match.Count > 2)
            {
                int firstreg = FindReg(line, 0);

                str.Append('0');

                str.Append("00000000");

                int secondreg = FindReg(line, 1);

                rgs[originReg].SetValue(rgs[firstreg].Value() | rgs[secondreg].Value());
            }
            else
            {
                int firstreg = FindReg(line, 0);

                str.Append('1');

                int toAdd = FindNumber(line);

                rgs[originReg].SetValue(rgs[firstreg].Value() | toAdd);
            }
            // PSR check at end
            State.ResC();
            if (rgs[originReg].Value() < 0)
            {
                State.SetN();
            }
            else State.ResN();
            if (rgs[originReg].Value() == 0 || originReg == 0)
            {
                State.SetZ();
            }
            else State.ResZ();
            State.ResV(); // bitwise operators cant produce overflow ( i hope )
        }
        void ORNCC(string line) // not sure how to interpret it 
        {
            //       int i NOR int v:
            //      i: 0000000000000000000001010
            //      v: 0000000000000000001011101
            //     or: 0000000000000000001011111
            // result: 1111111111111111110100000  (?)


            line = line.Replace("orncc ", "");
            str.Append("10");
            MatchCollection match = rxReg.Matches(line);
            int originReg = FindReg(line, 3);

            str.Append("010001"); // op

            if (match.Count > 2)
            {
                int firstreg = FindReg(line, 0);

                str.Append('0');

                str.Append("00000000");

                int secondreg = FindReg(line, 1);

                rgs[originReg].SetValue(~(rgs[firstreg].Value() | rgs[secondreg].Value()));

            }
            else
            {
                int firstreg = FindReg(line, 0);

                str.Append('1');

                int toAdd = FindNumber(line);

                rgs[originReg].SetValue(~(rgs[firstreg].Value() | toAdd)); 
            }
            // PSR check at end
            State.ResC();
            if (rgs[originReg].Value() < 0)
            {
                State.SetN();
            }
            else State.ResN();
            if (rgs[originReg].Value() == 0 || originReg == 0)
            {
                State.SetZ();
            }
            else State.ResZ();
            State.ResV(); // bitwise operators cant produce overflow ( i hope )
        }
        void ANDCC(string line)
        {
            line = line.Replace("andcc ", "");
            str.Append("10");
            MatchCollection match = rxReg.Matches(line);
            int originReg = FindReg(line, 3);

            str.Append("010001"); // op

            if (match.Count > 2)
            {
                int firstreg = FindReg(line, 0);

                str.Append('0');

                str.Append("00000000");

                int secondreg = FindReg(line, 1);

                rgs[originReg].SetValue(rgs[firstreg].Value() & rgs[secondreg].Value());
            }
            else
            {
                int firstreg = FindReg(line, 0);
                str.Append('1');

                int toAdd = FindNumber(line);

                rgs[originReg].SetValue(rgs[firstreg].Value() & toAdd);
            }

            // PSR check at end
            State.ResC();
            if (rgs[originReg].Value() < 0)
            {
                State.SetN();
            }
            else State.ResN();
            if (rgs[originReg].Value() == 0 || originReg == 0)
            {
                State.SetZ();
            }
            else State.ResZ();
            State.ResV(); // bitwise operators cant produce overflow ( i hope )
        }
        void SRL(string line)
        {
            line = line.Replace("srl ", "");
            str.Append("10");

            int originReg = FindReg(line, 3);
            str.Append("100110"); // op
            int firstreg = FindReg(line, 0);
           
            str.Append('1');

            int toAdd = FindNumber(line);

            rgs[originReg].SetValue(rgs[firstreg].Value() >> toAdd);
            // PSR does its work
            State.ResC();
            if (rgs[originReg].Value() < 0)
            {
                State.SetN();
            }
            else State.ResN();
            if (rgs[originReg].Value() == 0 || originReg == 0)
            {
                State.SetZ();
            }
            else State.ResZ();
            State.ResV(); // SRL cant produce overflow ( i hope )
        }
        void JUMPL(string line)
        {
            line = line.Replace("jumpl","");
            str.Append("10");
            str.Append("01111"); // target registry is always %r15
            str.Append("111000"); // op
            str.Append("00000"); // using %r0 as 2nd registry
            MatchCollection match = rxReg.Matches(line);
            int toadd;
            if (match.Count == 0)
            {
                str.Append('1'); // followed by simm13
                toadd = FindNumber(line);
            }
            else
            {
                str.Append('0');
                toadd = rgs[FindReg(line, 0)].Value();
            }           
            if (line.Split(' ')[1] == "+")
            {
                Link.SetValue(Link.Value() + toadd);               
            }
            else throw new Exception("Bad jumpl syntax");
            PC.SetValue(Func[Link.Value()].Value);
            State.SetJ();
        }
        void CALL(string line)
        {
            str.Append("01");
            string FuncToFind = line.Split(' ')[1];
            int f = 0;
            foreach(Memory m in Func)
            {
                if(m.Identifier == FuncToFind)
                {
                    string stradd = Convert.ToString(m.Value, 2);
                    for(int i = 0; i < 30 - stradd.Length; i++)
                    {
                        str.Append('0');
                    }
                    str.Append(stradd);
                    Link.SetValue(f);
                }
                f++;
            }
            PC.SetValue(Func[Link.Value()].Value);
        }
        int FindReg(string line, int which)
        {
            bool nullcheck = false;
            MatchCollection match = rxReg.Matches(line);
            if (which == 3)
            {
                nullcheck = true;
                which = match.Count - 1;
            }
            int firstreg = int.Parse(match[which].Value.Replace("%r", ""));
            if (firstreg > 31 || firstreg == 14 || firstreg == 15)
            {
                throw new FormatException("Invalid registry. You can call registries from 0 to 31, with the exception of 14 and 15.");
            }
            if (nullcheck)
            {
                if(firstreg == 0)
                {
                    State.OpNull = true;
                }               
            }
            for (int i = 0; i < 5 - Convert.ToString(firstreg, 2).Length; i++)
            {
                str.Append('0');
            }
            str.Append(Convert.ToString(firstreg, 2));

            return firstreg;
        }
        void FindReg(string line, int which, StringBuilder str2)
        {
            bool nullcheck = false;
            MatchCollection match = rxReg.Matches(line);
            if (which == 3)
            {
                nullcheck = true;
                which = match.Count - 1;
            }
            int firstreg = int.Parse(match[which].Value.Replace("%r", ""));
            if (firstreg > 31 || firstreg == 14 || firstreg == 15)
            {
                throw new FormatException("Invalid registry. You can call registries from 0 to 31, with the exception of 14 and 15.");
            }
            if (nullcheck)
            {
                if (firstreg == 0)
                {
                    State.OpNull = true;
                }
            }
            for (int i = 0; i < 5 - Convert.ToString(firstreg, 2).Length; i++)
            {
                str2.Append('0');
            }
            str2.Append(Convert.ToString(firstreg, 2));
        }
        int FindNumber(string line)
        {
            int toAdd = 0;
            bool found = false;
            for (int i = 0; i < 3; i++)
            {

                if (int.TryParse(line.Replace(" ", "").Split(',')[i], out toAdd))
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                throw new FormatException("Bad line syntax, missing integer / integer too big, use command 'help' for help.");
            }
            if(Convert.ToString(toAdd,2).Length > 13) // sethi implementation        0000 0000 00 00 0000 0000 00 | 00 0000 0000
            {                                                         //      sethi this part            // add this part
                StringBuilder str2 = new StringBuilder();
                str2.Append("00");
                FindReg(line, 3, str2);
                str2.Append("100");
                string add = Convert.ToString(toAdd, 2);
                while (32 > add.Length)
                {
                    add = "0" + add;
                }
                for (int l = 22, k = 0; l > 0; l--, k++)
                {
                    str2.Append(add[k]);
                }
                str.Append("000"); // unused bits ( sethi sets highest 22, and leaves lowest 10 for another op )
                for(int i = 10, k = 22; i > 0; i--, k++)
                {
                    str.Append(add[k]);
                }
                Console.WriteLine(str2.ToString());
                Console.WriteLine("sethi instruction assembled automatically above");
                return toAdd;                           
            }
            string stradd = Convert.ToString(toAdd, 2);
            for (int i = 0; i < 13 - stradd.Length; i++)
            {
                str.Append('0');
            }
            str.Append(stradd);

            return toAdd;
        }
        string GetLine(string fileName, int line)
        {
            using (var sr = new StreamReader(fileName))
            {
                for (int i = 0; i < line; i++)
                    sr.ReadLine();
                return sr.ReadLine();
            }
        }
    }
    class Memory // wip
    {
        public int Value { get; set; }
        public bool isFunc { get; set; }
        public string Identifier { get; set; }
        public Memory(int value, string identifier, bool f)
        {
            Value = value; Identifier = identifier; isFunc = f;
        }
        public override string ToString()
        {
            return $"[{Identifier}] stores {Value}, value is stored function location: {isFunc}";
        }
    }
    class PSR
    {
        public bool OpNull { get; set; } // if target registry was %r0
        bool n { get; set; } // if last operation yielded a negative number
        bool z { get; set; } // if last operation yielded 0
        bool v { get; set; } // overflow state
        bool j { get; set; } // check for jump
        bool c { get; set; } // i assume C means carry data from ALU to registry or vice-versa. 
        public PSR()
        {
            OpNull = false;
            n = false;
            z = false;
            v = false;
            j= false;
            c = false;
        }
        public void Reset()
        {
            n = false;
            z = false;
            v = false;
            c = false;
            j = false;
        }
        public bool N()
        {
            return n;
        }
        public bool Z()
        {
            return z;
        }
        public bool V()
        {
            return v;
        }
        public bool C()
        {
            return c;
        }
        public bool J()
        {
            return j;
        }
        public void SetZ()
        {
            z = true;
        }
        public void ResZ()
        {
            z = false;
        }
        public void SetN()
        {
            n = true;
        }
        public void ResN()
        {
            n = false;
        }
        public void SetV()
        {
            v = true;
        }
        public void ResV()
        {
            v = false;
        }
        public void SetC()
        {
            c = true;
        }
        public void ResC()
        {
            c = false;
        }
        public void SetJ()
        {
            j = true;
        }
        public void ResJ()
        {
            j = false;
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
            if (isNull) { Console.WriteLine("This registry is %r0 it will always contain 0."); }
            if (isSp) { Console.WriteLine("This registry is the Sp registry"); }
            if (isLink) { Console.WriteLine("This registry is the Link registry"); }
        }
        public void SetValue(int value)
        {
            this.value = value;
            string b = Convert.ToString(value, 2);
            bits = new char[32];
            for (int j = 0; j < bits.Length; j++)
            {
                bits[j] = '0';
            }
            for (int i = b.Length - 1, j = bits.Length - 1; i >= 0; i--, j--)
            {
                bits[j] = b[i];
            }
        }
    }    
}
