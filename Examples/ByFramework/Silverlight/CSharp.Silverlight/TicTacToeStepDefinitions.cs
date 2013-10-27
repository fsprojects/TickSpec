namespace CSharpSilverlightUnitTesting
{
    using TickSpec;

    public class TicTacToeStepDefinitions
    {
        [Given(@"a board layout:")] 
        public void GivenABoardLayout(Table table)
        {
        }

        [When(@"a player marks (X|O) at (top|middle|bottom) (left|middle|right)")]
        public void WhenPlayer(string mark, string row, string col)
        {
        }

        [Then(@"(X|O) wins")]
        public void ThenWins(string mark)
        {
        }
    }
}
