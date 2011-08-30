using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SetVision.Gamelogic.Combinatorics;

namespace SetVision.Gamelogic
{
    public class Logic
    {
        public Logic()
        {}

        public List<Card> GenerateCards()
        {
            List<Card> cards = new List<Card>();
            foreach (Color color in Enum.GetValues(typeof(Color)))
            {
                foreach (Fill fill in Enum.GetValues(typeof(Fill)))
                {
                    foreach (Shape shape in Enum.GetValues(typeof(Shape)))
                    {
                        for (int i = 1; i <= 3; i++)
                        {
                            cards.Add(new Card(color, shape, fill, i));
                        }
                    }
                }
            }

            return cards;
        }

        public bool AttributeSame(List<Card> group, string attribute)
        {
            string first = group[0][attribute];
            foreach (Card card in group)
            {
                if (card[attribute] != first)
                {
                    return false;
                }
            }
            return true;
        }

        public bool AttributeDifferent(List<Card> group, string attribute)
        {
            //string first = group[0][attribute];
            HashSet<string> vals = new HashSet<string>();
            foreach (Card card in group)
            {
                bool ok = vals.Add(card[attribute]);
                if (!ok)
                {
                    return false;
                }
            }
            return true;
        }

        public HashSet<List<Card>> FindSets(List<Card> cards)
        {
            HashSet<List<Card>> sets = new HashSet<List<Card>>();

            List<List<Card>> possible_sets = ListExtensions<Card>.Combinations(cards, 3);
            foreach(List<Card> possible_set in possible_sets)
            {
                if(IsSet(possible_set))
                {
                    sets.Add(possible_set);
                }
            }

            return sets;
        }

        private bool IsSet(List<Card> possible_set)
        {
            string[] attributes = { "color","shape","fill","count" };
            foreach (string attr in attributes)
            {
                if (!(AttributeSame(possible_set, attr) || AttributeDifferent(possible_set, attr)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
