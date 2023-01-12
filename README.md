# ARC machine ( based on a specific SPARC command set ): 
![image](https://user-images.githubusercontent.com/115028239/212092603-15a370b1-5d26-4353-82d5-574c50b4ecfe.png)

So you might want to read through this before trying to play around it.

Important parsing notes: 
1.Whenever using "ld" or "st" the memory label has to be in square brackets like: "[memID]", otherwise it won't work. (more on this at commands)
1.1 Using parsing it like [MEMID] in any other command will result in a compilation error, because only the commands LD and ST can access memory.
2. Use blank spaces after each word, an example would be: "function1: ld 25, %r5 ! loads 25 into registry 5", comments are optional.
3. Memory labels that store integers need to be either jumped over or jumped to a different label to avoid errors. The machine cannot compile a line that doesn't have a command in it.

How the machine actually works: ( i tried my best )

-This machine was not tested properly for complicated bits of code, it is meant for a simple visualization of simple assembly commands ( almost ).
-This machine stores Labels and Memory values inside a special file it creates, then it reads from it.
-It works somewhat on a line-by-line read type, it does a read-through on execution to find memory fragments, and have references to labels ( memory fragments that store integers have their label store the location as well ).
-Because the labels stored in memory are stored as integers , the machine output code, the 32 bit long code you'll see on your screen might not be accurate.

How to code in this:

Using memory commands:

ld // loads a register with either an integer or a value from a memory label (has to exist inside the code) 
example: ld 25, %r5 ! loads 25 into registry 5
or if you have a memory label called "m" stored already in memory you can use "ld [m], %r5" to load it's value into r5.

st // stores a register value into a memory label / fragment
example: st %r5, [m] // if m exists it gets overwritten, otherwise it creates m and stores it in memory

Using arithmetic commands: 

addcc // adds int to a register value, or two register values, stores it into the last registry specified
example: addcc %r5, 7, %r6 // r6 <-- r5 + 7 // int can be negative ( not suggested )
         addcc %r5, %r4, %r6 // r6 <-- r5 + r4
         
Using logic commands:

- these commands work the same as "addcc"
andcc // bitwise operator &
orcc // bitwise operator |
orncc // bitwise operator ~(a|b) // might not work properly
srl // shift right logical >> 

Using call , conditional branches, jumpl commands:

call // calls a label
example: call label1 // this will move the execution to label1 , this command leaves the caller line value in the Link registry, which can be be used with:

jumpl // jumps to link registry line value + line offset ( use this to get back to the part where you called a label, or use a branch command to skip stuff )
example: jumpl + 1 // it's important to write it with spaces and use at least 1, jumpl cannot be used to jump back ( yet ), jumpl depends on link value, so be careful how you use it, link value can only be set with call ( which sets it at calling position, or branch command which sets it at the label where you jump to ).

ba // branch always, use this to jump to label and have link value set at label position
example: ba function1

bvs // branch only if the last operator resulted in an ARITHMETHIC overflow. if it didn't the next line instruction is executed.
bcs // branch only if there was carry in the last operator ( no clue what this means it doesn't even work because PSR doesn't get a carry set )
be // branch only if last operation resulted in 0.
bneg // branch only if the last operation resulted in a negative number.

A quick code snippet visualization:

ld 2, %r5                      // 1. this gets executed first
func1: addcc %r5, 6, %r9       // 2. moves onto this
call func3                     // 3. func3 is called, link is set to line 3
func2: addcc %r8, 2, %r8       // 6. because of the jumpl + 1 ( link remains at line 3 , be very very careful here because a bad command will make the program loop forever ).
ba done                        // 7. branch always means we jump to label "done:" , branch command sets link to the label position so link will be in this case 8.
func3: addcc %r5, 6, %r8       // 4. this is executed because of the call
jumpl + 1                      // 5. jumpl + 1 means the program goes back to 3 + 1 = 4 th line, so the next executed line will be: 6.
done: jumpl + 2                // 8. jumpl + 2 means we jump: 8 + 2 = 10, which is out of bounds of the program so it stops and skips over func4: label and instruction.
func4: andcc %r5, 6, %r8       // this never gets executed because of the ba done, command. 
.end                           // this doesn't get compiled , it's just a way to run the program.

Other commands:
.debug ( shows what the registers store, and what memory labels are / what they store.
.help ( send you here ).
.reset ( this resets the register and memory of the machine, it is good to use this command after each code execution, otherwise you need to leave yourself a line to jump to to continue your code ).

I tried replicating this part of the SPARC machine based on things learned at P.O.C.A. course @UOradea.

