using System;
using System.Collections.Generic;
using System.Text;

public static partial class game_engine
{
    public static readonly string[] OpNames = new string[]
    {
        "DONE",

        "MUL_F",
        "MUL_V", 
        "MUL_FV",
        "MUL_VF",
 
        "DIV",

        "ADD_F",
        "ADD_V", 
  
        "SUB_F",
        "SUB_V",

        "EQ_F",
        "EQ_V",
        "EQ_S", 
        "EQ_E",
        "EQ_FNC",
 
        "NE_F",
        "NE_V", 
        "NE_S",
        "NE_E", 
        "NE_FNC",
 
        "LE",
        "GE",
        "LT",
        "GT", 

        "INDIRECT",
        "INDIRECT",
        "INDIRECT", 
        "INDIRECT", 
        "INDIRECT",
        "INDIRECT", 

        "ADDRESS", 

        "STORE_F",
        "STORE_V",
        "STORE_S",
        "STORE_ENT",
        "STORE_FLD",
        "STORE_FNC",

        "STOREP_F",
        "STOREP_V",
        "STOREP_S",
        "STOREP_ENT",
        "STOREP_FLD",
        "STOREP_FNC",

        "RETURN",
  
        "NOT_F",
        "NOT_V",
        "NOT_S", 
        "NOT_ENT", 
        "NOT_FNC", 
  
        "IF",
        "IFNOT",
  
        "CALL0",
        "CALL1",
        "CALL2",
        "CALL3",
        "CALL4",
        "CALL5",
        "CALL6",
        "CALL7",
        "CALL8",
  
        "STATE",
  
        "GOTO", 
  
        "AND",
        "OR", 

        "BITAND",
        "BITOR"
    };


    public static bool pr_trace;
    public static prstack_t[] pr_stack = new prstack_t[q_shared.MAX_STACK_DEPTH];
    public static int pr_depth;

    public static int[] localstack = new int[q_shared.LOCALSTACK_SIZE];
    public static int localstack_used;
    public static dfunction_t pr_xfunction;
    public static int pr_xstatement;
    public static int pr_argc;

    public static void PR_Profile_f()
    {
        if (pr_functions == null)
            return;
            
        dfunction_t best;
        int num = 0;
        do
        {
            int max = 0;
            best = null;
            for (int i = 0; i < pr_functions.Length; i++)
            {
                dfunction_t f = pr_functions[i];
                if (f.profile > max)
                {
                    max = f.profile;
                    best = f;
                }
            }
            if (best != null)
            {
                if (num < 10)
                    Con_Printf("{0,7} {1}\n", best.profile, GetString(best.s_name));
                num++;
                best.profile = 0;
            }
        } while (best != null);
    }
    public unsafe static void PR_ExecuteProgram(int fnum)
    {
        if (fnum < 1 || fnum >= pr_functions.Length)
        {
            if (pr_global_struct.self != 0)
                ED_Print(PROG_TO_EDICT(pr_global_struct.self));
            Host_Error("PR_ExecuteProgram: NULL function");
        }

        dfunction_t f = pr_functions[fnum];

        int runaway = 100000;
        pr_trace = false;

        // make a stack frame
        int exitdepth = pr_depth;

        int ofs;
        int s = PR_EnterFunction(f);
        edict_t ed;
            
        while (true)
        {
            s++;	// next statement

            eval_t* a = (eval_t*)Get(pr_statements[s].a);
            eval_t* b = (eval_t*)Get(pr_statements[s].b);
            eval_t* c = (eval_t*)Get(pr_statements[s].c);

            if (--runaway == 0)
                PR_RunError("runaway loop error");

            pr_xfunction.profile++;
            pr_xstatement = s;

            if (pr_trace)
                PR_PrintStatement(ref pr_statements[s]);

            switch ((OP)pr_statements[s].op)
            {
                case OP.OP_ADD_F:
                    c->_float = a->_float + b->_float;
                    break;
                    
                case OP.OP_ADD_V:
                    c->vector[0] = a->vector[0] + b->vector[0];
                    c->vector[1] = a->vector[1] + b->vector[1];
                    c->vector[2] = a->vector[2] + b->vector[2];
                    break;

                case OP.OP_SUB_F:
                    c->_float = a->_float - b->_float;
                    break;

                case OP.OP_SUB_V:
                    c->vector[0] = a->vector[0] - b->vector[0];
                    c->vector[1] = a->vector[1] - b->vector[1];
                    c->vector[2] = a->vector[2] - b->vector[2];
                    break;

                case OP.OP_MUL_F:
                    c->_float = a->_float * b->_float;
                    break;

                case OP.OP_MUL_V:
                    c->_float = a->vector[0] * b->vector[0]
                            + a->vector[1] * b->vector[1]
                            + a->vector[2] * b->vector[2];
                    break;

                case OP.OP_MUL_FV:
                    c->vector[0] = a->_float * b->vector[0];
                    c->vector[1] = a->_float * b->vector[1];
                    c->vector[2] = a->_float * b->vector[2];
                    break;

                case OP.OP_MUL_VF:
                    c->vector[0] = b->_float * a->vector[0];
                    c->vector[1] = b->_float * a->vector[1];
                    c->vector[2] = b->_float * a->vector[2];
                    break;

                case OP.OP_DIV_F:
                    c->_float = a->_float / b->_float;
                    break;

                case OP.OP_BITAND:
                    c->_float = (int)a->_float & (int)b->_float;
                    break;

                case OP.OP_BITOR:
                    c->_float = (int)a->_float | (int)b->_float;
                    break;


                case OP.OP_GE:
                    c->_float = (a->_float >= b->_float) ? 1 : 0;
                    break;

                case OP.OP_LE:
                    c->_float = (a->_float <= b->_float) ? 1 : 0;
                    break;

                case OP.OP_GT:
                    c->_float = (a->_float > b->_float) ? 1 : 0;
                    break;

                case OP.OP_LT:
                    c->_float = (a->_float < b->_float) ? 1 : 0;
                    break;

                case OP.OP_AND:
                    c->_float = (a->_float != 0 && b->_float != 0) ? 1 : 0;
                    break;

                case OP.OP_OR:
                    c->_float = (a->_float != 0 || b->_float != 0) ? 1 : 0;
                    break;

                case OP.OP_NOT_F:
                    c->_float = (a->_float != 0) ? 0 : 1;
                    break;

                case OP.OP_NOT_V:
                    c->_float = (a->vector[0] == 0 && a->vector[1] == 0 && a->vector[2] == 0) ? 1 : 0;
                    break;

                case OP.OP_NOT_S:
                    c->_float = (a->_string == 0 || String.IsNullOrEmpty(GetString(a->_string))) ? 1 : 0;
                    break;

                case OP.OP_NOT_FNC:
                    c->_float = (a->function == 0) ? 1 : 0;
                    break;

                case OP.OP_NOT_ENT:
                    c->_float = (PROG_TO_EDICT(a->edict) == sv.edicts[0]) ? 1 : 0;
                    break;

                case OP.OP_EQ_F:
                    c->_float = (a->_float == b->_float) ? 1 : 0;
                    break;

                case OP.OP_EQ_V:
                    c->_float = ((a->vector[0] == b->vector[0]) &&
                        (a->vector[1] == b->vector[1]) &&
                        (a->vector[2] == b->vector[2])) ? 1 : 0;
                    break;

                case OP.OP_EQ_S:
                    c->_float = (GetString(a->_string) == GetString(b->_string)) ? 1 : 0; //!strcmp(pr_strings + a->_string, pr_strings + b->_string);
                    break;

                case OP.OP_EQ_E:
                    c->_float = (a->_int == b->_int) ? 1 : 0;
                    break;

                case OP.OP_EQ_FNC:
                    c->_float = (a->function == b->function) ? 1 : 0;
                    break;


                case OP.OP_NE_F:
                    c->_float = (a->_float != b->_float) ? 1 : 0;
                    break;

                case OP.OP_NE_V:
                    c->_float = ((a->vector[0] != b->vector[0]) ||
                        (a->vector[1] != b->vector[1]) || (a->vector[2] != b->vector[2])) ? 1 : 0;
                    break;

                case OP.OP_NE_S:
                    c->_float = (GetString(a->_string) != GetString(b->_string)) ? 1 : 0; //strcmp(pr_strings + a->_string, pr_strings + b->_string);
                    break;

                case OP.OP_NE_E:
                    c->_float = (a->_int != b->_int) ? 1 : 0;
                    break;

                case OP.OP_NE_FNC:
                    c->_float = (a->function != b->function) ? 1 : 0;
                    break;

                case OP.OP_STORE_F:
                case OP.OP_STORE_ENT:
                case OP.OP_STORE_FLD:		// integers
                case OP.OP_STORE_S:
                case OP.OP_STORE_FNC:		// pointers
                    b->_int = a->_int;
                    break;

                case OP.OP_STORE_V:
                    b->vector[0] = a->vector[0];
                    b->vector[1] = a->vector[1];
                    b->vector[2] = a->vector[2];
                    break;

                case OP.OP_STOREP_F:
                case OP.OP_STOREP_ENT:
                case OP.OP_STOREP_FLD:		// integers
                case OP.OP_STOREP_S:
                case OP.OP_STOREP_FNC:		// pointers
                    ed = EdictFromAddr(b->_int, out ofs);
                    ed.StoreInt(ofs, a);
                    break;

                case OP.OP_STOREP_V:
                    ed = EdictFromAddr(b->_int, out ofs);
                    ed.StoreVector(ofs, a);
                    break;

                case OP.OP_ADDRESS:
                    ed = PROG_TO_EDICT(a->edict);
                    if (ed == sv.edicts[0] && sv.active)
                        PR_RunError("assignment to world entity");
                    c->_int = MakeAddr(a->edict, b->_int);
                    break;

                case OP.OP_LOAD_F:
                case OP.OP_LOAD_FLD:
                case OP.OP_LOAD_ENT:
                case OP.OP_LOAD_S:
                case OP.OP_LOAD_FNC:
                    ed = PROG_TO_EDICT(a->edict);
                    ed.LoadInt(b->_int, c);
                    break;

                case OP.OP_LOAD_V:
                    ed = PROG_TO_EDICT(a->edict);
                    ed.LoadVector(b->_int, c);
                    break;

                case OP.OP_IFNOT:
                    if (a->_int == 0)
                        s += pr_statements[s].b - 1;	// offset the s++
                    break;

                case OP.OP_IF:
                    if (a->_int != 0)
                        s += pr_statements[s].b - 1;	// offset the s++
                    break;

                case OP.OP_GOTO:
                    s += pr_statements[s].a - 1;	// offset the s++
                    break;

                case OP.OP_CALL0:
                case OP.OP_CALL1:
                case OP.OP_CALL2:
                case OP.OP_CALL3:
                case OP.OP_CALL4:
                case OP.OP_CALL5:
                case OP.OP_CALL6:
                case OP.OP_CALL7:
                case OP.OP_CALL8:
                    pr_argc = pr_statements[s].op - (int)OP.OP_CALL0;
                    if (a->function == 0)
                        PR_RunError("NULL function");

                    dfunction_t newf = pr_functions[a->function];

                    if (newf.first_statement < 0)
                    {
                        // negative statements are built in functions
                        int i = -newf.first_statement;
                        if (i >= pr_builtins.Length)
                            PR_RunError("Bad builtin call number");
                        Execute(i);
                        break;
                    }

                    s = PR_EnterFunction(newf);
                    break;

                case OP.OP_DONE:
                case OP.OP_RETURN:
                    float* ptr = (float*)_GlobalStructAddr;
                    int sta = pr_statements[s].a;
                    ptr[q_shared.OFS_RETURN + 0] = *(float*)Get(sta);
                    ptr[q_shared.OFS_RETURN + 1] = *(float*)Get(sta + 1);
                    ptr[q_shared.OFS_RETURN + 2] = *(float*)Get(sta + 2);

                    s = PR_LeaveFunction();
                    if (pr_depth == exitdepth)
                        return;		// all done
                    break;

                case OP.OP_STATE:
                    ed = PROG_TO_EDICT(pr_global_struct.self);
#if FPS_20
                    ed->v.nextthink = pr_global_struct->time + 0.05;
#else
                    ed.v.nextthink = pr_global_struct.time + 0.1f;
#endif
                    if (a->_float != ed.v.frame)
                    {
                        ed.v.frame = a->_float;
                    }
                    ed.v.think = b->function;
                    break;

                default:
                    PR_RunError("Bad opcode %i", pr_statements[s].op);
                    break;
            }
        }
    }
    public static unsafe int PR_EnterFunction(dfunction_t f)
    {
        pr_stack[pr_depth].s = pr_xstatement;
        pr_stack[pr_depth].f = pr_xfunction;
        pr_depth++;
        if (pr_depth >= q_shared.MAX_STACK_DEPTH)
            PR_RunError("stack overflow");

        // save off any locals that the new function steps on
        int c = f.locals;
        if (localstack_used + c > q_shared.LOCALSTACK_SIZE)
            PR_RunError("PR_ExecuteProgram: locals stack overflow\n");

        for (int i = 0; i < c; i++)
            localstack[localstack_used + i] = *(int*)Get(f.parm_start + i);
        localstack_used += c;

        // copy parameters
        int o = f.parm_start;
        for (int i = 0; i < f.numparms; i++)
        {
            for (int j = 0; j < f.parm_size[i]; j++)
            {
                Set(o, *(int*)Get(q_shared.OFS_PARM0 + i * 3 + j));
                o++;
            }
        }

        pr_xfunction = f;
        return f.first_statement - 1;	// offset the s++
    }
    public static void PR_RunError(string fmt, params object[] args)
    {
        PR_PrintStatement(ref pr_statements[pr_xstatement]);
        PR_StackTrace();
        Con_Printf(fmt, args);

        pr_depth = 0;		// dump the stack so host_error can shutdown functions

        Host_Error("Program error");
    }
    public static void PR_StackTrace()
    {
        if (pr_depth == 0)
        {
            Con_Printf("<NO STACK>\n");
            return;
        }

        pr_stack[pr_depth].f = pr_xfunction;
        for (int i = pr_depth; i >= 0; i--)
        {
            dfunction_t f = pr_stack[i].f;

            if (f == null)
            {
                Con_Printf("<NO FUNCTION>\n");
            }
            else
                Con_Printf("{0,12} : {1}\n", GetString(f.s_file), GetString(f.s_name));
        }
    }
    public static void PR_PrintStatement(ref dstatement_t s)
    {
        if (s.op < OpNames.Length)
        {
            Con_Printf("{0,10} ", OpNames[s.op]);
        }

        OP op = (OP)s.op;
        if (op == OP.OP_IF || op == OP.OP_IFNOT)
            Con_Printf("{0}branch {1}", PR_GlobalString(s.a), s.b);
        else if (op == OP.OP_GOTO)
        {
            Con_Printf("branch {0}", s.a);
        }
        else if ((uint)(s.op - OP.OP_STORE_F) < 6)
        {
            Con_Printf(PR_GlobalString(s.a));
            Con_Printf(PR_GlobalStringNoContents(s.b));
        }
        else
        {
            if (s.a != 0)
                Con_Printf(PR_GlobalString(s.a));
            if (s.b != 0)
                Con_Printf(PR_GlobalString(s.b));
            if (s.c != 0)
                Con_Printf(PR_GlobalStringNoContents(s.c));
        }
        Con_Printf("\n");
    }
    public static int PR_LeaveFunction ()
    {
        if (pr_depth <= 0)
            Sys_Error("prog stack underflow");

        // restore locals from the stack
        int c = pr_xfunction.locals;
        localstack_used -= c;
        if (localstack_used < 0)
            PR_RunError("PR_ExecuteProgram: locals stack underflow\n");

        for (int i = 0; i < c; i++)
        {
            Set(pr_xfunction.parm_start + i, localstack[localstack_used + i]);
            //((int*)pr_globals)[pr_xfunction->parm_start + i] = localstack[localstack_used + i];
        }

        // up stack
        pr_depth--;
        pr_xfunction = pr_stack[pr_depth].f;
            
        return pr_stack[pr_depth].s;
    }
    public static int MakeAddr(int prog, int offset)
    {
        return ((prog & 0xFFFF) << 16) + (offset & 0xFFFF);
    }
    public static edict_t EdictFromAddr(int addr, out int ofs)
    {
        int prog = (addr >> 16) & 0xFFFF;
        ofs = addr & 0xFFFF;
        return PROG_TO_EDICT(prog);
    }
}