using Microsoft;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using VerifyCS = NO_BASE_CALL.Test.CSharpCodeFixVerifier<
    NO_BASE_CALL.NO_BASE_CALLAnalyzer,
    NO_BASE_CALL.NO_BASE_CALLCodeFixProvider>;

namespace NO_BASE_CALL.Test
{
    [TestClass]
    public class NO_BASE_CALLUnitTest
    {
        [TestMethod]
        public async Task TestMethod1()
        {
            var test = @"public class A 
{
   public virtual void Foo() {}
}
public class B1 : A 
{ 
   public override void Foo()
   { 
      base.Foo(); 
   } 
}
public class B2 : A 
{ 
   public override void Foo()  
   { 
      ;
   } 
}";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        
        [TestMethod]
        public async Task TestMethod2()
        {
            var test = @"
public class A 
{
   public virtual void Foo() {}
}
public class B1 : A 
{ 
   public override void Foo() 
   { 
      base.Foo(); 
   } 
}
public class B2 : A 
{ 
   public override void Foo()  
   { 
      base.Foo(); 
   } 
}
public class B3 : A 
{ 
   public void Foo()  
   { 
      ;
   } 
}";

            await VerifyCS.VerifyAnalyzerAsync(test);
        }
        [TestMethod]
        public async Task TestMethod3()
        {
            var test = @"
public class A 
{
   public virtual void Foo() {}
}
public class B1 : A 
{ 
   public void Foo() 
   { 
      base.Foo(); 
   } 
}
public class B2 : A 
{ 
   public void Foo()  
   { 
      ; 
   } 
}
public class B3 : A 
{ 
   public void Foo()  
   { 
      ;
   } 
}";


            await VerifyCS.VerifyAnalyzerAsync(test);
        }

        [TestMethod]
        public async Task TestMethod4()
        {
            string testCode = @"
public class A 
{
   public virtual void Foo() {}
}
public class B1 : A 
{ 
   public override void Foo() 
   { 
      base.Foo(); 
   } 
}
public class B2 : A 
{ 
   public override void Foo()  
   { 
      base.Foo(); 
   } 
}
public class B3 : A 
{ 
   public override void Foo()  
   { 
      ;
   } 
}";
            var expected = VerifyCS.Diagnostic().WithSpan(22, 25, 22, 28).WithMessage("The implementation of method Foo() doesn't call corresponding method Foo() from the base class, but usually (more than 65% cases) it does it (2 out of 3 times).");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }
        [TestMethod]
        public async Task TestMethod5()
        {
            string testCode = @"
public class A 
{
   public virtual void Foo() {}
}
public class B1 : A 
{ 
   public override void Foo() 
   { 
      base.Foo(); 
   } 
}
public class B2 : A 
{ 
   public override void Foo()  
   { 
      base.Foo(); 
   } 
}
public class B3 : A 
{ 
   public override void Foo()  
   { 
      ;
   } 
}
public class B4 : A 
{ 
   public override void Foo()  
   { 
    int x = 0;
    for (int i = 0; i < 27; i++) base.Foo(); 
   } 
}";

            var expected = VerifyCS.Diagnostic().WithSpan(22, 25, 22, 28).WithMessage("The implementation of method Foo() doesn't call corresponding method Foo() from the base class, but usually (more than 65% cases) it does it (3 out of 4 times).");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected);
        }
        [TestMethod]
        public async Task TestMethod6()
        {
            string testCode = @"
public class A
{
    public virtual void Foo() { } 
}
public class B1 : A
{
    public override void Foo()  
    {
        base.Foo();
    }
}
public class B2 : A
{
    public override void Foo()  
    {
        base.Foo();
    }
}
public class B5 : A
{
    public override void Foo()  
    {
        base.Foo();
    }
}
public class B6 : A
{
    public override void Foo() 
    {
        base.Foo();
    }
}
public class B3 : A
{
    public override void Foo() 
    {
        ;
    }
}
public class B4 : A
{
    public override void Foo()      
    {
        for (int i = 0; i < 10; i++)
        {
            ;
        }
    }
}";

            var expected_1 = VerifyCS.Diagnostic().WithSpan(36, 26, 36, 29).WithMessage("The implementation of method Foo() doesn't call corresponding method Foo() from the base class, but usually (more than 65% cases) it does it (4 out of 6 times).");
            var expected_2 = VerifyCS.Diagnostic().WithSpan(43, 26, 43, 29).WithMessage("The implementation of method Foo() doesn't call corresponding method Foo() from the base class, but usually (more than 65% cases) it does it (4 out of 6 times).");
            await VerifyCS.VerifyAnalyzerAsync(testCode, expected_1, expected_2);
        }

    }

}
