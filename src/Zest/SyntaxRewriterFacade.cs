using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Zest {

    internal class SyntaxRewriter : CSharpSyntaxRewriter {

        private static MethodRewriter _methodRewriter = new MethodRewriter();               // if we have 100 instances of SyntaxRewriterFacade, does MethodRewriter need instantiating
                                                                                            // 100 times? No, it holds no state and is no different from one to the next
                                                                                            // If that were ever to change, remove the static keyword and have one per instance. 

        public override SyntaxNode VisitMethodDeclaration(MethodDeclarationSyntax node) {
            return _methodRewriter.foo(node);
        }

    }

}
