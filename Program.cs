using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackJack
{
    public static class GameRules
    {
        public const int MaxHands = 3;
        public const int DealerStandScore = 17;
        public const int BlackjackScore = 21;
    }

    public enum Stats { InGame, Lose, Stand }
    public enum EndReason { DealerBlackjack, DealerBusted, NormalEnd }

    public class Hand
    {
        public int Number { get; set; }
        public List<int> Cards { get; set; } = new();
        public Stats Status { get; set; } = Stats.InGame;

        public bool CanSplit() => Cards.Count == 2 && Cards[0] == Cards[1];

        public int CalculateScore()
        {
            int score = Cards.Where(c => c != 1).Sum();
            int aces = Cards.Count(c => c == 1);

            for (int i = 0; i < aces; i++)
                score += score + 11 <= GameRules.BlackjackScore ? 11 : 1;

            return score;
        }

        public bool IsBusted() => CalculateScore() > GameRules.BlackjackScore;
    }

    public abstract class CardHolder
    {
        public List<Hand> Hands { get; set; } = new();

        public void AddHand(List<int> cards)
        {
            if (Hands.Count >= GameRules.MaxHands)
                throw new InvalidOperationException($"Нельзя иметь больше {GameRules.MaxHands}-х рук");

            Hands.Add(new Hand { Number = Hands.Count + 1, Cards = new List<int>(cards) });
        }
    }

    public class Player : CardHolder
    {
        public string Name { get; set; } = "Игрок";

        public void SplitHand(Hand handToSplit, Deck deck)
        {
            if (!handToSplit.CanSplit() || Hands.Count >= GameRules.MaxHands)
                throw new InvalidOperationException("Нельзя разделить эту руку");

            var hand1 = new List<int> { handToSplit.Cards[0], deck.DealCard() };
            var hand2 = new List<int> { handToSplit.Cards[1], deck.DealCard() };

            Hands.Remove(handToSplit);
            AddHand(hand1);
            AddHand(hand2);

            for (int i = 0; i < Hands.Count; i++)
                Hands[i].Number = i + 1;
        }
    }

    public class Dealer : CardHolder
    {
        public string Name { get; } = "Дилер";
    }

    public class Deck
    {
        private readonly Stack<int> _cards = new();
        private readonly Random _random = new();

        public void Fill()
        {
            _cards.Clear();
            const int numberOfPictureCards = 96;
            const int numberOfOtherCards = 25;

            for (int card = 1; card < 10; card++)
                for (int i = 0; i < numberOfOtherCards; i++)
                    _cards.Push(card);

            for (int i = 0; i < numberOfPictureCards; i++)
                _cards.Push(10);

            Shuffle();
        }

        private void Shuffle()
        {
            var arr = _cards.ToArray();
            _cards.Clear();
            foreach (var card in arr.OrderBy(_ => _random.Next()))
                _cards.Push(card);
        }

        public int DealCard()
        {
            if (_cards.Count == 0) throw new InvalidOperationException("Колода пуста");
            return _cards.Pop();
        }
    }

    public class Game
    {
        private readonly List<Player> _players = new();
        private readonly Dealer _dealer;
        private readonly Deck _deck;

        public Game(Dealer dealer, Deck deck)
        {
            _dealer = dealer;
            _deck = deck;
        }

        public void AddPlayers(int count)
        {
            for (int i = 1; i <= count; i++)
            {
                var player = new Player { Name = $"Игрок {i}" };
                player.AddHand(new List<int> { _deck.DealCard(), _deck.DealCard() });
                _players.Add(player);
            }

            _dealer.AddHand(new List<int> { _deck.DealCard(), _deck.DealCard() });
        }

        public void Play()
        {
            if (DealerHasBlackjack())
            {
                ShowDealerCards(true);
                EndGame(EndReason.DealerBlackjack);
                return;
            }

            foreach (var player in _players)
                PlayerTurn(player);

            var reason = DealerTurn();
            EndGame(reason);
        }

        private bool DealerHasBlackjack() =>
            _dealer.Hands.First().Cards.Count == 2 &&
            _dealer.Hands.First().CalculateScore() == GameRules.BlackjackScore;

        private void PlayerTurn(Player player)
        {
            foreach (var hand in player.Hands.Where(h => h.Status == Stats.InGame).ToList())
            {
                while (hand.Status == Stats.InGame)
                {
                    Console.WriteLine($"\n{player.Name}, рука {hand.Number}: [{string.Join(", ", hand.Cards)}] Очки: {hand.CalculateScore()}");
                    Console.WriteLine("Выберите: 1 - взять карту, 2 - остановиться, 3 - разделить");
                    var choice = Console.ReadLine();

                    switch (choice)
                    {
                        case "1":
                            hand.Cards.Add(_deck.DealCard());
                            if (hand.IsBusted()) { hand.Status = Stats.Lose; Console.WriteLine("Перебор!"); }
                            break;

                        case "2":
                            hand.Status = Stats.Stand;
                            break;

                        case "3":
                            if (hand.CanSplit()) player.SplitHand(hand, _deck);
                            else Console.WriteLine("Нельзя разделить эту руку");
                            break;

                        default:
                            Console.WriteLine("Неверный ввод");
                            break;
                    }
                }
            }
        }

        private EndReason DealerTurn()
        {
            var hand = _dealer.Hands.First();
            while (hand.CalculateScore() < GameRules.DealerStandScore)
            {
                hand.Cards.Add(_deck.DealCard());
                ShowDealerCards(true);
            }
            return hand.CalculateScore() > GameRules.BlackjackScore ? EndReason.DealerBusted : EndReason.NormalEnd;
        }

        private void EndGame(EndReason reason)
        {
            Console.WriteLine("\n--- ИГРА ОКОНЧЕНА ---");

            switch (reason)
            {
                case EndReason.DealerBlackjack:
                    Console.WriteLine("Блэкджек дилера!");
                    break;
                case EndReason.DealerBusted:
                    Console.WriteLine("Дилер перебрал!");
                    break;
                case EndReason.NormalEnd:
                    Console.WriteLine("Игра завершена.");
                    break;
            }

            ShowDealerCards(true);
            ShowResults(reason);
        }

        private void ShowDealerCards(bool showAll)
        {
            var hand = _dealer.Hands.First();
            if (showAll)
                Console.WriteLine($"\n{_dealer.Name} карты: [{string.Join(", ", hand.Cards)}] Очки: {hand.CalculateScore()}");
            else
                Console.WriteLine($"\n{_dealer.Name} карты: [{hand.Cards[0]}, **]");
        }

        private void ShowResults(EndReason reason)
        {
            int dealerScore = _dealer.Hands.First().CalculateScore();

            foreach (var player in _players)
            {
                foreach (var hand in player.Hands)
                {
                    int handScore = hand.CalculateScore();

                    switch ((hand.Status, handScore > dealerScore, reason))
                    {
                        case (Stats.Lose, _, _):
                            Console.WriteLine($"{player.Name} проиграл руку {hand.Number} с очками {handScore}");
                            break;

                        case (Stats.InGame or Stats.Stand, true, EndReason.DealerBusted):
                        case (Stats.InGame or Stats.Stand, true, _):
                            Console.WriteLine($"{player.Name} выиграл руку {hand.Number} с очками {handScore}");
                            break;

                        case (Stats.InGame or Stats.Stand, false, _):
                            Console.WriteLine($"{player.Name} завершил руку {hand.Number} с очками {handScore}");
                            break;
                    }
                }
            }

            Console.WriteLine(dealerScore <= GameRules.BlackjackScore &&
                              _players.All(p => p.Hands.All(h => h.CalculateScore() <= dealerScore))
                ? "Дилер победил"
                : "Дилер проиграл");
        }
    }

    internal class Program
    {
        static void Main()
        {
            Console.WriteLine("Добро пожаловать в Блэкджек!");
            Console.Write("Введите количество игроков (1-5): ");

            if (!int.TryParse(Console.ReadLine(), out int count) || count < 1 || count > 5)
            {
                Console.WriteLine("Неверное количество игроков!");
                return;
            }

            var deck = new Deck();
            deck.Fill();
            var dealer = new Dealer();
            var game = new Game(dealer, deck);

            game.AddPlayers(count);
            game.Play();

            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }
    }
}
