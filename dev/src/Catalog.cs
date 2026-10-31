using System;
using System.Collections.Generic;

namespace MathSymbolPad {
    public sealed class Symbol {
        public string Id, Label, Text, Hint;
        public int CaretIndex;
        public Symbol(string id, string label, string text, string hint, int caretIndex = -1) {
            Id=id; Label=label; Text=text; Hint=hint; CaretIndex=caretIndex<0?text.Length:caretIndex;
        }
    }
    public sealed class SymbolPage {
        public string Name; public int Columns; public Symbol[] Items;
        public SymbolPage(string name,int columns,params Symbol[] items) { Name=name;Columns=columns;Items=items; }
    }
    public static class Catalog {
        static Symbol S(string id,string text,string hint) { return new Symbol(id,text,text,hint); }
        static Symbol T(string id,string label,string text,string hint,int caret) { return new Symbol(id,label,text,hint,caret); }
        public static readonly SymbolPage[] Pages = {
            new SymbolPage("常用",6,
                S("le","≤","小于等于"),S("ge","≥","大于等于"),S("ne","≠","不等于"),S("infinity","∞","无穷"),S("union","∪","并集"),S("intersection","∩","交集"),
                S("member","∈","属于"),S("not-member","∉","不属于"),S("subset","⊆","包含于，可以相等"),S("empty","∅","空集"),S("sqrt-symbol","√","根号"),S("square","²","平方，接在字母或数后面"),
                S("minus","−","减号或负号"),S("plus-minus","±","正负号"),S("multiply","×","乘号"),S("divide","÷","除号"),S("pi","π","圆周率"),S("degree","°","度"),
                S("left-square","[","左方括号"),S("right-square","]","右方括号"),S("left-round","(","左圆括号"),S("right-round",")","右圆括号"),S("left-curly","{","左花括号"),S("right-curly","}","右花括号")),
            new SymbolPage("集合",6,
                S("set-in","∈","属于"),S("set-not-in","∉","不属于"),S("set-subset","⊆","包含于，可以相等"),S("proper-subset","⊂","真包含于"),S("superset","⊇","包含，可以相等"),S("proper-superset","⊃","真包含"),
                S("set-union","∪","并集"),S("set-intersection","∩","交集"),S("set-empty","∅","空集"),S("complement","∁","补集符号，配合全集使用"),S("difference","∖","集合差"),S("condition","|","描述法中的满足条件"),
                S("natural","ℕ","自然数集"),S("integer","ℤ","整数集"),S("rational","ℚ","有理数集"),S("real","ℝ","实数集"),S("set-equal","=","集合相等"),S("set-not-equal","≠","集合不相等"),
                S("set-left","{","集合左花括号"),S("set-right","}","集合右花括号"),S("comma",",","分隔元素或区间端点"),S("set-infinity","∞","无穷"),S("set-left-square","[","包含左端点"),S("set-right-square","]","包含右端点")),
            new SymbolPage("代数",6,
                S("alg-plus","+","加号"),S("alg-minus","−","减号或负号"),S("alg-times","×","乘号"),S("alg-divide","÷","除号"),S("dot","·","乘号或数量积"),S("alg-pm","±","正负号"),
                S("less","<","小于"),S("greater",">","大于"),S("alg-le","≤","小于等于"),S("alg-ge","≥","大于等于"),S("alg-ne","≠","不等于"),S("approximately","≈","约等于"),
                S("alg-root","√","平方根号"),S("cube-root","∛","立方根号"),S("alg-square","²","平方"),S("cube","³","立方"),T("power","xⁿ","^()","幂，输入后在括号内填写指数",2),T("subscript","aₙ","_()","下标，输入后在括号内填写",2),
                S("implies","⇒","推出"),S("equivalent","⇔","等价"),T("function","f(x)","f()","函数值，在括号里填写输入",2),T("probability","P(A)","P()","事件概率，在括号里填写事件",2),S("sequence","a_n","数列第n项，普通文本记法"),S("sum","S_n","数列前n项和，普通文本记法")),
            new SymbolPage("几何",6,
                S("geo-pi","π","圆周率"),S("geo-degree","°","度"),S("theta","θ","西塔，常用于角"),S("alpha","α","阿尔法"),S("beta","β","贝塔"),S("angle","∠","角"),
                S("perpendicular","⊥","垂直"),S("parallel","∥","平行"),S("triangle","△","三角形"),S("congruent","≅","全等"),S("similar","∽","相似"),S("geo-approx","≈","约等于"),
                T("sin","sin","sin()","正弦，在括号内填写角",4),T("cos","cos","cos()","余弦，在括号内填写角",4),T("tan","tan","tan()","正切，在括号内填写角",4),S("vector-a","a⃗","向量a"),S("vector-b","b⃗","向量b"),S("geo-dot","·","向量数量积"),
                T("log","log","log()","对数，普通文本记法",4),T("ln","ln","ln()","自然对数",3),S("factorial","!","阶乘"),S("sigma","Σ","求和符号"),S("x-bar","x̄","平均数"),S("variance","s²","方差")),
            new SymbolPage("模板",4,
                T("pair-round","( )","()","圆括号，光标停在中间",1),T("pair-square","[ ]","[]","方括号，光标停在中间",1),T("pair-curly","{ }","{}","花括号，光标停在中间",1),T("absolute","|x|","||","绝对值，光标停在中间",1),
                T("root-template","√( )","√()","根号，光标停在括号内",2),T("fraction","a/b","()/()","分数，先填写分子，再移动到分母",1),T("exponent","xⁿ","^()","接在底数后，填写指数",2),T("subscript-template","aₙ","_()","接在字母后，填写下标",2),
                T("interval-closed","[a,b]","[,]","闭区间，先填写左端点",1),T("interval-left","[a,b)","[,)","左闭右开区间，先填写左端点",1),T("interval-right","(a,b]","(,]","左开右闭区间，先填写左端点",1),T("interval-open","(a,b)","(,)","开区间，先填写左端点",1),
                T("negative-infinity","−∞ →","(−∞,)","向负无穷延伸，填写右端点",4),T("positive-infinity","→ +∞","(,+∞)","向正无穷延伸，填写左端点",1),T("two-sides","两侧并集","(−∞,]∪[,+∞)","两侧含端点区间，先填写左侧的有限端点",4),T("coordinate","(x,y)","(,)","坐标，先填写横坐标",1))
        };
        public static Symbol Find(string id) { foreach(var p in Pages)foreach(var s in p.Items)if(s.Id==id)return s;throw new ArgumentException("未知符号："+id); }
    }
}
