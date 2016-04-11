using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.MSBuild;
using System;

namespace Zest {
    class Program {
        
        // auto-stubbing, automagically. 
        // in ctors or public properties, unless you override, they get stubs that do nothing, because why waste time populating them all

        // auto virtualising
        // auto unsealing
        // auto exposing setter

        // statics - convert static members to instance. convert static ctor to regular ctor.
        // leave class as static and provide a static accessor for the instance of the singleton
        //    then, ........???? Code that uses this? That sets it
        //    ... overload all methods with the paramter of this static type and have the existing ones forward on to this call with new StaticThingy() ?  .... not sure that would work
        //    ... Proxy it into a globally accessibly namespace and replace references with that one..
        //    ... Then code could do Static.MyStaticThingamabob = 42, and the code under test would read back whatever you put. Method monkey patching same thing.
        //    ... Make Static a new instance for each testcase - that way reinitialised every time, you can then override as you please
        //    ... Effectively "hoisting" every static thing into a single class all of their own. 


        static void Main(string[] args) {

            if(args.Length != 1) {
                throw new ArgumentException("Expected filename to transform");
            }

            var fileToCompile = args[0];
            var source = File.ReadAllText(fileToCompile);

            var stringText = SourceText.From(source, Encoding.UTF8);
            var tree = SyntaxFactory.ParseSyntaxTree(source);



            var rewritten = new SyntaxRewriter().Visit(tree.GetRoot());



            // Load SLN. 
            //MSBuildWorkspace
            //Microsoft.CodeAnalysis.worksapces
            //Workspace w;
            //Microsoft.CodeAnalysis.Workspace.TryGetWorkspace(new SourceTextContainer(), out w);
            var w = MSBuildWorkspace.Create();



            return;

            FindMethods(tree);

            FindStaticMembers(tree);

            var parsetree = CSharpSyntaxTree.ParseText(stringText);

            //MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            //Solution solution = workspace.OpenSolutionAsync(solutionFilePath).Result;

            var rt = tree.GetRoot(); // as CSharpCompilationUnit;

            var internalToken = SyntaxFactory.Token(SyntaxKind.InternalKeyword);
            var publicToken = SyntaxFactory.Token(SyntaxKind.PublicKeyword);
            var getToken = SyntaxFactory.Token(SyntaxKind.GetKeyword);

            //var n = rt.DescendantNodes()
            //    .OfType<PropertyDeclarationSyntax>()
            //    .SelectMany(p => p.Modifiers)
            //    .First();

            var publicProperties = rt.DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .Where(p => p.Modifiers.Any(m => m.ValueText == publicToken.Text))
                .ToList();



            //var str = CreateAutoSetter();

            // private properties ignored altogether
            
            // non-private, non static properties.....
            // setter not public (private, protected, or internal)
            // setter not even there -> rewrite get expr into variable read, add setter to set this var in test code
            //            -- or, rewrite as func execution, and let the setter be assigned as a func in testing code (probably too much logic in tests)

            var unexposedSetters = publicProperties.Select(p => p.AccessorList.Accessors.SingleOrDefault(a => a.Keyword.Text != getToken.Text)).ToList();

            // remove existing setter, if any
            var e = publicProperties[0].AccessorList.WithAccessors(new SyntaxList<AccessorDeclarationSyntax> { CreateAutoSetter() });

            var e2 = publicProperties[0].AccessorList.AddAccessors(CreateAutoSetter());

            var e3 = publicProperties[0].AccessorList.RemoveNodes(publicProperties[0].AccessorList.DescendantNodes(), SyntaxRemoveOptions.KeepNoTrivia);
            //.InsertNodesAfter(null, new []{ CreateAutoSetter() });

            string s = publicProperties[0].ToFullString();
            // 
            // 
            // 
            // GetAccessorDeclaration


            // code says (or is rewritten to read):
            // var now = DateTime.Now -> Statics.DateTime.Now;
            //
            // Your test code looks like:
            // Statics.DateTime.Now = new DateTime(2015, 12, 05, 15, 00, 00);            // <---- fix the current time
            // Statics.Reset();                                                          // Undo overrides test cases have made and put them back to their values as lifted when code was originalky rewritten. 

            //n2.First().Modifiers.Add(publicToken);

            //var sd=rt.DescendantTokens();
            //var classMems = rt.Members[0].Members[0].Members;

            //PropertyDeclarationSyntax
            //var p = rt.me
//            var parseTree = new SyntaxTree
////CompilationUnitSyntax root = tree.GetRoot();

        }

        private static void FindStaticMembers(SyntaxTree document) {

            var rt = document.GetRoot();
            var things = rt.DescendantNodes().OfType<MemberDeclarationSyntax>().ToList();

            var staticToken = SyntaxFactory.Token(SyntaxKind.StaticKeyword);
            var staticThings = rt.DescendantNodes()
                .OfType<PropertyDeclarationSyntax>()
                .Where(d => d.Modifiers.Any(m => m.Text == staticToken.Text))
                .ToList();
            
            //MemberDeclarationSyntax md;
            //PropertyDeclarationSyntax pd;

        }

        private static void FindMethods(SyntaxTree document)
        {
            var rt = document.GetRoot();
            var methods = rt.DescendantNodes().OfType<MethodDeclarationSyntax>();


            var publicToken = SyntaxFactory.Token(SyntaxKind.PublicKeyword);
            //var varDec = SyntaxFactory.VariableDeclaration(SyntaxFactory.ty
            //var oe = SyntaxFactory.LocalDeclarationStatement(
            //    //modifiers: new SyntaxTokenList { publicToken },
            //    declaration: SyntaxFactory.VariableDeclaration(
            //        variables: new SeparatedSyntaxList<VariableDeclaratorSyntax>(SyntaxFactory.VariableDeclaration
            //        type: SyntaxFactory.IdentifierName("int")));

            var parse = SyntaxFactory.ParseStatement("public Func<int, object, byte, char> MyFunc = (a, b, c) => { kreturn 42 };");
            parse = SyntaxFactory.ParseStatement("public int x = 12;");

            //SyntaxFactory.FieldDeclaration(new SyntaxList<AttributeListSyntax>(), new SyntaxTokenList { publicToken }, null);
        }


        private void GetExposedPropertiesWithRestrictedSetter(SyntaxTree document) {

        }


        private static bool IsExposed(BasePropertyDeclarationSyntax nonTerminalExpression) {

            var publicToken = SyntaxFactory.Token(SyntaxKind.PublicKeyword);

            // has access modifier public
            //   -> true
            return !nonTerminalExpression.Modifiers.Any(m => m.Text == publicToken.Text);
            //nonTerminalExpression.
            
            // else
            // -> false
        }


        /// <summary>
        /// Creates an auto-implemented setter (i.e. with the backing variable created by the compiler - the programmer specifies only get; set; )
        /// </summary>
        private static AccessorDeclarationSyntax CreateAutoSetter() {

            //var a = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetKeyword);
            var b = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration);

            //b.

            return b;

        }


        /// <summary>
        /// Takes a method declaration and re-writes it as a Func/Action that can be reassigned in code
        /// </summary>
        private static void EnableMonkeyPatch()
        {
            
        }


    }
}
