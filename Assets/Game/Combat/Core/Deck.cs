using System;
using System.Collections.Generic;
using ProjectS.Data;

namespace ProjectS.Combat.Core
{
    /// <summary>Chồng rút, tay bài và chồng bỏ của một nhân vật.</summary>
    public class Deck
    {
        private readonly List<CardData> drawPile = new List<CardData>();
        private readonly List<CardData> hand = new List<CardData>();
        private readonly List<CardData> discardPile = new List<CardData>();
        private readonly Random rng;

        public IReadOnlyList<CardData> DrawPile => drawPile;
        public IReadOnlyList<CardData> Hand => hand;
        public IReadOnlyList<CardData> DiscardPile => discardPile;
        public int TotalCount => drawPile.Count + hand.Count + discardPile.Count;

        public Deck(IEnumerable<CardData> cards, Random rng)
        {
            this.rng = rng ?? throw new ArgumentNullException(nameof(rng));
            drawPile.AddRange(cards);
            Shuffle(drawPile);
        }

        /// <summary>Rút tối đa <paramref name="count"/> thẻ. Hết chồng rút thì xáo chồng bỏ thành chồng rút mới.</summary>
        public int Draw(int count)
        {
            int drawn = 0;
            for (int i = 0; i < count; i++)
            {
                if (drawPile.Count == 0)
                {
                    if (discardPile.Count == 0) break;
                    drawPile.AddRange(discardPile);
                    discardPile.Clear();
                    Shuffle(drawPile);
                }

                int last = drawPile.Count - 1;
                hand.Add(drawPile[last]);
                drawPile.RemoveAt(last);
                drawn++;
            }
            return drawn;
        }

        /// <summary>Chuyển một thẻ từ tay sang chồng bỏ. Trả về false nếu thẻ không ở trên tay.</summary>
        public bool Discard(CardData card)
        {
            if (!hand.Remove(card)) return false;
            discardPile.Add(card);
            return true;
        }

        public void DiscardHand()
        {
            discardPile.AddRange(hand);
            hand.Clear();
        }

        private void Shuffle(List<CardData> cards)
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                CardData temp = cards[i];
                cards[i] = cards[j];
                cards[j] = temp;
            }
        }
    }
}
