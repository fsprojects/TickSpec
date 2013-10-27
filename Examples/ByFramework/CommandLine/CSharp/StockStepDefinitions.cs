namespace TickSpec.Examples.CSharp
{
   using TickSpec;
   using System.Diagnostics;

   public class StockStepDefinitions
   {
      private StockItem _stockItem;

      [Given(@"a customer buys a black jumper")]
      public void GivenACustomerBuysABlackJumper()
      {
          GivenIHaveNBlackJumpersLeftInStock(1);
      }

      [Given(@"I have (.*) black jumper left in stock")]
      [Given(@"I have (.*) black jumpers left in stock")]
      public void GivenIHaveNBlackJumpersLeftInStock(int n)
      {
         _stockItem = new StockItem() { Count = n };  
      }

      [When(@"he returns the jumper for a refund")]
      public void WhenHeReturnsTheJumperForARefund()
      {
         _stockItem.Count += 1;
      }

      [Then(@"I should have (.*) black jumpers in stock")]
      public void ThenIShouldHaveNBlackJumpersInStock(int n)
      {
         Debug.Assert(_stockItem.Count == n);
      }
   }
}
