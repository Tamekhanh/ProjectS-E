namespace ProjectS.Data
{
    /// <summary>10 rate của thẻ, từ thấp (Bet) tới cao (Aleph).</summary>
    public enum Rate
    {
        Bet,
        Gimel,
        Dalet,
        He,
        Vav,
        Zayin,
        Het,
        Tet,
        Yod,
        Aleph
    }

    public static class RateRules
    {
        /// <summary>Số bản trùng tối đa của một thẻ trong một deck.</summary>
        public static int MaxCopies(Rate rate)
        {
            if (rate <= Rate.He) return 3;
            if (rate <= Rate.Het) return 2;
            return 1;
        }
    }
}
