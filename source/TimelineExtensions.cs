using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
//using NfString = NoFuture.Util.Core.NfString;

namespace NoFuture.Timeline
{
    public static class TimelineExtensions
    {
        public static RegexOptions MyRegexOptions { get; set; } = RegexOptions.IgnoreCase;

        public static string ToStringUnixNewline(this StringBuilder stringBuilder)
        {
            return stringBuilder.ToString()
                .Replace(new string(new[] {(char) 0x0D, (char) 0x0A}), new string(new[] {Config.GraphChars.UNIX_NL_CHAR}));
        }

        public static TextCanvas ToTextCanvas(this IRuleEntry entry, Rule ruler)
        {
            if(ruler == null)
                throw new NoRuleSetException();

            var entryIndex = ruler.CalcEntryIndex(entry);
            var entryLines = entry.ToString().Split(Config.GraphChars.UNIX_NL_CHAR);

            var ruleIndex = ruler.GetIndexRule();

            //iteration is on this difference so there needs to be at least that many lines in the string
            if (entryIndex.Item2 - entryIndex.Item1 > entryLines.Length)
            {
                throw new DrawingException(
                    string.Format(
                        "Entry is indexed at '{0}' to '{1}', a difference of '{2}' while the entry as a string is composed of '{3}' lines",
                        entryIndex.Item1,
                        entryIndex.Item2,
                        (entryIndex.Item2 - entryIndex.Item1),
                        entryLines.Length));
            }

            var textCanvas = new TextCanvas
            {
                Ruler = ruler,
                Items = new List<TextItem>(),
                MinIndex = entryIndex.Item1,
                MaxIndex = entryIndex.Item2,
                StartValue = entry.StartValue,
                EndValue = entry.EndValue,
                Width =  entry.Width,
                Id = entry.Id
            };

            //single line entry
            if (entryIndex.Item2 == entryIndex.Item1)
            {
                if (entryIndex.Item1 < 0 || entryIndex.Item1 >= ruleIndex.Count)
                    throw new DrawingException(
                        string.Format(
                            "The entry by ID '{0}' having the text value of '{1}' " +
                            "is set to a position that is beyond the current ruler's max.",
                            entry.Id, entry));

                textCanvas.Items.Add(new TextItem
                {
                    HashMarkValue = ruleIndex[entryIndex.Item1],
                    Index = entryIndex.Item1,
                    Text = entry.ToString().ToCharArray().ToList()
                });
                return textCanvas;
            }

            //multiple line entry
            for (var i = entryIndex.Item1; i <= entryIndex.Item2; i++)
            {
                var hashMark = ruleIndex[i];
                var text = entryLines[(i - entryIndex.Item1)];

                if (string.IsNullOrEmpty(text))
                    continue;
                var nti = new TextItem
                {
                    HashMarkValue = hashMark,
                    Index = i,
                    Text = text.ToCharArray().ToList()
                };
                nti.Ranges.Add(new TextRange(){Id = entry.Id,Length = nti.Text.Count, StartIndex = 0});
                textCanvas.Items.Add(nti);
            }

            return textCanvas;
        }

        public static TextCanvas Merge(this TextCanvas entry1Tc, TextCanvas entry2Tc, Rule ruler)
        {
            //check for null args
            if (ruler == null || entry2Tc == null)
                throw new DrawingException("A merge requires a source, a candidate and a ruler");

            //validate args have something to work with
            if(entry1Tc.IsEmptyItems() && entry2Tc.IsEmptyItems())
                return new TextCanvas();

            if (entry2Tc.IsEmptyItems() && !entry1Tc.IsEmptyItems())
                return entry1Tc;

            if (entry1Tc.IsEmptyItems() && !entry2Tc.IsEmptyItems())
                return entry2Tc;
            

            //determine total edges for the whole
            var entriesMin = entry1Tc.MinIndex < entry2Tc.MinIndex ? entry1Tc.MinIndex : entry2Tc.MinIndex;
            var entriesMax = entry1Tc.MaxIndex > entry2Tc.MaxIndex ? entry1Tc.MaxIndex : entry2Tc.MaxIndex;
            
            Func<TextItem, bool> tti = ti1 => ti1 != null && !ti1.IsEmptyText();

            var textCanvas = new TextCanvas
            {
                Ruler = ruler,
                Items = new List<TextItem>(),
                MaxIndex = entriesMax,
                MinIndex = entriesMin,
                Width = entry1Tc.Width > entry2Tc.Width ? entry1Tc.Width : entry2Tc.Width
            };
            for (var i = entriesMin; i <= entriesMax; i++)
            {
                var entry1i = entry1Tc.Items.FirstOrDefault(item => item.Index == i);
                var entry2i = entry2Tc.Items.FirstOrDefault(item => item.Index == i);

                if (entry1i == null && entry2i == null)
                    continue;

                //merge line
                if (tti(entry1i) && tti(entry2i))
                {
                    var newEntry = entry1i.Copy();
                    newEntry.Ranges.AddRange(entry2i.Ranges);
                    newEntry.Text =
                        MergeString(new string(entry1i.Text.ToArray()), new string(entry2i.Text.ToArray()))
                            .ToCharArray().Where(c => Convert.ToInt32(c) >= 0x20)
                            .ToList();
                    textCanvas.Items.Add(newEntry);

                    continue;
                }

                if (tti(entry1i))
                {
                    textCanvas.Items.Add(entry1i.Copy());
                    continue;
                }
                textCanvas.Items.Add(entry2i.Copy());

            }
            return textCanvas;
        }

        public static TextCanvas Concat(this TextCanvas block1Tc, TextCanvas block2Tc, Rule ruler)
        {
            if(ruler == null)
                throw new NoRuleSetException();
            
            if (block2Tc == null)
            {
                return block1Tc;
            }

            var minIndex = block1Tc.MinIndex < block2Tc.MinIndex ? block1Tc.MinIndex : block2Tc.MinIndex;
            var maxIndex = block1Tc.MaxIndex > block2Tc.MaxIndex ? block1Tc.MaxIndex : block2Tc.MaxIndex;
            var blockBarCount = 0;

            Func<char[], bool> printBar =
                c =>
                    new string(c).EndsWith($"{Config.GraphChars.Rail}{Config.GraphChars.BarRailIntersect}");

            var tc = new TextCanvas
            {
                MaxIndex = maxIndex,
                Ruler = ruler,
                MinIndex = minIndex,
                Id = block1Tc.Id + Config.GraphChars.IDSeparator + block2Tc.Id
            };
            for (var i = minIndex; i <= maxIndex; i++)
            {
                var ti1 = block1Tc.GetTextOrEmpty(i, block1Tc.Width);
                var ti2 = block2Tc.GetTextOrEmpty(i, block2Tc.Width);

                if (ti1.Index < 0 || ti2.Index < 0)
                    continue;

                var ntxt = new List<char>(ti1.Text);
                if (blockBarCount == 1 || block2Tc.MaxIndex >= i)
                    ntxt.Add(Config.GraphChars.Bar);
                else
                    ntxt.Add((char)0x20);

                if (printBar(ti1.Text.ToArray()))
                    blockBarCount += 1;

                var txtRng = new TextRange {Id = block2Tc.Id, Length = ti2.Text.Count, StartIndex = ntxt.Count};
                txtRng.InnerRanges.AddRange(ti2.Ranges.Where(x => x.Id != block2Tc.Id));
                ntxt.AddRange(ti2.Text);

                var nti = new TextItem {HashMarkValue = ti1.HashMarkValue, Index = i, Text = ntxt};
                nti.Ranges.AddRange(ti1.Ranges);
                nti.Ranges.Add(txtRng);
                tc.Items.Add(nti);

                if (ntxt.Count > tc.Width)
                    tc.Width = ntxt.Count;
            }
            return tc;
        }

        /// <summary>
        /// Examples 
        ///  ((789,797),(804,816)) as "789-97\804-16"
        ///  ((1797,1803)) as "1797-1803"
        ///  ((640, 609)) as "640-609" 
        ///  ((1588,null)) as "1588-"
        /// </summary>
        /// <param name="years"></param>
        /// <param name="startValue"></param>
        /// <param name="endValue"></param>
        /// <returns></returns>
        /// <remarks>
        /// BCE values do NOT have the century removed
        /// </remarks>
        public static string PrintYearsRange(IEnumerable<Tuple<double?, double?>> years, double startValue,
            double endValue)
        {

            Func<Tuple<double?, double?>, string> yearFmt =
                tuple => $"{tuple.Item1}-{tuple.Item2}";

            Func<double?, double, double?> remCentury;
            //CE
            if (startValue < endValue)
            {
                remCentury = (y, c) => y - c < 100 ? y - c : y;
            }
            else//BCE
            {
                remCentury = (y, c) => y;
            }

            years = years ?? new List<Tuple<double?, double?>>();

            //for tuple item 1 in the same century as item 2, we only want last two digits
            var yyyy = years.ToList();

            var pCentury = 0D;
            var yy = new List<Tuple<double?, double?>>();
            for (var i = 0; i < yyyy.Count; i++)
            {
                var ccyy = yyyy[i].Item1;
                var century = ccyy.GetValueOrDefault(0) - ccyy.GetValueOrDefault(0) % 100;

                var yyyy1 = yyyy[i].Item1 == null ? null : remCentury(yyyy[i].Item1, century);
                var yyyy2 = yyyy[i].Item2 == null ? null : remCentury(yyyy[i].Item2, century);

                yy.Add(new Tuple<double?, double?>(i == 0 || pCentury != century ? yyyy[i].Item1 : yyyy1, yyyy2));

                pCentury = century;
            }
            var yearLst = years.ToList();
            var isSingleYear = yearLst.Count == 1 &&
                               Math.Abs(yearLst[0].Item1.GetValueOrDefault(0D) - yearLst[0].Item2.GetValueOrDefault(0D)) <= 1D;

            return isSingleYear ? $"{yy[0].Item1}\\{yy[0].Item2}" : string.Join("\\", yy.Select(x => yearFmt(x)));
        }

        /// <summary>
        /// The counterpart to <see cref="PrintYearsRange"/>
        /// </summary>
        /// <param name="printyears"></param>
        /// <param name="startValue"></param>
        /// <param name="endValue"></param>
        /// <returns></returns>
        public static IEnumerable<Tuple<double?, double?>> ParseYearsRange(string printyears, double startValue,
            double endValue)
        {
            if (string.IsNullOrWhiteSpace(printyears) ||
                !System.Text.RegularExpressions.Regex.IsMatch(printyears, "[0-9\x2F\x5C\x2d]+?") || 
                (Math.Abs(startValue) < 0.001 && Math.Abs(endValue) < 0.001))
                return new List<Tuple<double?, double?>>();

            var century = startValue - startValue%100;

            var lowerBound = startValue < endValue ? startValue : endValue;
            var upperBound = startValue < endValue ? endValue : startValue;

            //century is removed from most of the string values for CE years
            Func<double, bool> inBounds = v => (lowerBound <= v) && (v <= upperBound);

            var aYears = new List<Tuple<double?, double?>>();
            //now to transform back to tuples
            var splitYears = printyears.Contains("/") ? printyears.Split('/') : printyears.Split('\\');
            foreach (var t in splitYears) 
            {
                var u = new[] {string.Empty, string.Empty};
                var tt =
                    t.Split('-') //dash is each item therein
                        .Union(u.Select(y => y)).ToArray();
                double y1;
                double y2;
                double? yy1 = null;
                double? yy2 = null;
                if (double.TryParse(tt[0], out y1))
                {
                    if (!inBounds(y1))
                        y1 += century;
                    yy1 = y1;
                    century = y1 - y1%100;
                }
                if (double.TryParse(tt[1], out y2))
                {
                    if (!inBounds(y2))
                        y2 += century;
                    yy2 = y2;
                }

                aYears.Add(new Tuple<double?, double?>(yy1, yy2));
            }
            if(aYears.Count <= 1 || aYears.All(x => x.Item1.HasValue && x.Item2.HasValue))
                return aYears;

            var consolidateYears = new List<Tuple<double?, double?>>();
            for (var i = 0; i < aYears.Count; i += 2)
            {
                if (i == aYears.Count - 1)
                    continue;
                consolidateYears.Add(new Tuple<double?, double?>(aYears[i].Item1, aYears[i+1].Item1));
            }

            return consolidateYears;
        }

        /// <summary>
        /// Pads a string on both sides with spaces so that the 
        /// sum of the whole is equal to <see cref="printBlockWidth"/>.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="printBlockWidth"></param>
        /// <returns></returns>
        /// <example>
        /// <![CDATA[
        /// PrintInCenter(22,"hello");//'         hello        ' the single-quotes are added for the example.
        /// ]]>
        /// </example>
        public static string PrintInCenter(this string text, int printBlockWidth)
        {
            //validate the args
            if (printBlockWidth < 0.0D)
                throw new ArgumentException("the width must be a positive integer");

            var printBlock = String.Format(("{0,-" + printBlockWidth + "}"), " ").ToCharArray();


            if (String.IsNullOrWhiteSpace(text))
                return new string(printBlock);

            //set the text block to have an even number of items
            var textBlock = text.ToCharArray();
            if (textBlock.Length % 2 == 1)
            {
                textBlock = String.Format(" {0}", text).ToCharArray();
            }

            if (textBlock.Length >= printBlockWidth)
                return new string(textBlock);

            //find mid-point of the block
            var blockMidPt = Math.Ceiling((double)printBlockWidth / 2);

            //find mid-point of the text
            var textMidPt = Math.Ceiling((double)text.Length / 2);

            /*
             * textBlock =  | |J|u|d|e|a|
             *               0 1 2 3 4 5
             * printBlock = | | | | | | | | | | | |
             *               0 1 2 3 4 5 6 7 8 9 A
             * pbMidPt = 5
             * tbMidPt = 3
                 
             *  0 -> printBlock[0+5] = textBlock[0+3],
             *        printBlock[5-0] = textBlock[3-0]
             *  1 -> printBlock[1+5] = textBlock[1+3],
             *        printBlock[(5-1)-1] = textBlock[(3-1)-1]
             *  2 -> printBlock[2+5] = textBlock[2+3],
             *        printBlock[(5-1)-2] = textBlock[(3-1)-2]
             */

            for (var i = 0; i < textMidPt; i++)
            {
                printBlock[i + (int)blockMidPt] = textBlock[i + (int)textMidPt];
                printBlock[(int)blockMidPt - 1 - i] = textBlock[(int)textMidPt - 1 - i];
            }

            return new string(printBlock);
        }

        /// <summary>
        /// Takes two strings and lays one atop the other by ordinal giving precedence 
        /// to characters above 0x20.
        /// </summary>
        /// <param name="primaryString"></param>
        /// <param name="secondaryString"></param>
        /// <returns></returns>
        /// <example>
        /// <![CDATA[
        /// MergeString("|  this is primary ","AND            SECONDARY HERE");//|NDthis is primaryONDARY HERE
        /// ]]>
        /// </example>
        public static string MergeString(this string primaryString, string secondaryString)
        {
            if (String.IsNullOrEmpty(primaryString) && String.IsNullOrEmpty(secondaryString))
                return String.Empty;

            Func<string, int, char> valOr0 = (c, i) => String.IsNullOrEmpty(c) || c.Length <= i ? (char)0x0 : c[i];

            Func<string, string, int, char> primOrSec = (s, s1, arg3) =>
            {
                if (arg3 < 0) return (char)0x0;

                var pChar = valOr0(primaryString, arg3);

                //if the first char did not come back as \0 then make the second char \0
                var sChar = Convert.ToInt32(pChar) != 0x0 ? (char)0x0 : valOr0(secondaryString, arg3);

                //special circumstance to return secondary when primary is a space
                if (valOr0(primaryString, arg3) <= (char)0x20 && valOr0(secondaryString, arg3) > (char)0x20)
                    return valOr0(secondaryString, arg3);

                return (char)(Convert.ToInt32(pChar) | Convert.ToInt32(sChar));
            };

            var length = (primaryString?.Length ?? 0) > secondaryString.Length
                ? (primaryString?.Length ?? 0)
                : secondaryString.Length;

            var mergedChars = new char[length];
            for (var i = 0; i < length; i++)
            {
                mergedChars[i] = primOrSec(primaryString, secondaryString, i);
            }

            return new string(mergedChars);
        }

        /// <summary>
        /// Test the <see cref="subject"/> against the <see cref="pattern"/>
        /// using the <see cref="MyRegexOptions"/>.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="pattern"></param>
        /// <param name="match"></param>
        /// <param name="matchGroup">The index value of the Groups array within the first Matches</param>
        /// <returns>
        /// The value of the first group.
        /// </returns>
        public static bool IsRegexMatch(string subject, string pattern, out string match, int matchGroup = 0)
        {
            match = null;
            if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(pattern))
                return false;

            var regex = new Regex(pattern, MyRegexOptions);

            if (!regex.IsMatch(subject))
                return false;

            var grp = regex.Matches(subject)[0];
            if (!grp.Success)
                return false;
            match = grp.Groups[matchGroup].Value;
            return true;

        }
    }
}