using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace StockQuote {
    public class StockQuote {
        public DateTime Date { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }

        // Little methods useful - increase readability of code
        // responsibilities in right place
        public bool ReversesDownFrom(StockQuote otherQuote) {
            // Is Opening price of this StockQuote greater than day High of other quote - ie gone up overnight
            // and Closing price of this StockQuote less than day Low of other quote - ie crashed out during day (compare to yesterday)..BAD!
            return Open > otherQuote.High && Close < otherQuote.Low;
        }

        public bool ReversesUpFrom(StockQuote otherQuote) {
            return Open < otherQuote.Low && Close > otherQuote.High;
        }
    }

    // Responsible for loading from a file
    public class StockQuoteLoader {
        private readonly string _fileName;

        public StockQuoteLoader(string fileName) {
            _fileName = fileName;
        }

        public IEnumerable<StockQuote> Load() {
            return
                from line in File.ReadAllLines(_fileName).Skip(1)
                let data = line.Split(',')
                select new StockQuote {
                    //Date = DateTime.Parse(data[0]),
                    Date = DateTime.ParseExact(data[0], "M/d/yyyy", CultureInfo.InvariantCulture),
                    Open = decimal.Parse(data[1]),
                    High = decimal.Parse(data[2]),
                    Low = decimal.Parse(data[3]),
                    Close = decimal.Parse(data[4])
                };
        }
    }

    // Enum describing the direction
    public enum ReversalDirection {
        Up,
        Down
    }

    public class Reversal {
        public Reversal(StockQuote quote, ReversalDirection direction) {
            StockQuote = quote;
            Direction = direction;
        }
        public ReversalDirection Direction { get; set; }
        public StockQuote StockQuote { get; set; }
    }

    // Dedicated to going through stocks, invoking ReversesDownFrom or UpFrom and
    // passes onto something else
    // only loops thorugh stocks and calls a method
    public class ReversalLocator {
        private readonly IList<StockQuote> _quotes;

        public ReversalLocator(IList<StockQuote> quotes) {
            _quotes = quotes;
        }

        public IEnumerable<Reversal> Locate() {
            for (int i = 0; i < _quotes.Count - 1; i++) {
                if (_quotes[i].ReversesDownFrom(_quotes[i + 1])) {
                    yield return new Reversal(_quotes[i], ReversalDirection.Down);
                }
                if (_quotes[i].ReversesUpFrom(_quotes[i + 1])) {
                    yield return new Reversal(_quotes[i], ReversalDirection.Up);
                }
            }
        }
    }

    // Collaborate with and orchestrate other objects to get a result
    class StockQuoteAnalyzer {
        private readonly StockQuoteLoader _loader;
        private List<StockQuote> _quotes;

        public StockQuoteAnalyzer(string fileName) {
            _loader = new StockQuoteLoader(fileName);
            _quotes = _loader.Load().ToList();
        }

        public IEnumerable<Reversal> FindReversals() {
            var locator = new ReversalLocator(_quotes);
            return locator.Locate();
        }
    }

    class Program {
        static void Main(string[] args) {
            var analyzer = new StockQuoteAnalyzer("msft.csv");
            foreach (var reversal in analyzer.FindReversals()) {
                PrintReversal(reversal);
            }
        }

        private static void PrintReversal(Reversal reversal) {
            if (reversal.Direction == ReversalDirection.Up) {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Reversed up on " + reversal.StockQuote.Date);
            }
            else if (reversal.Direction == ReversalDirection.Down) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Reversed down on " + reversal.StockQuote.Date);
            }
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}

