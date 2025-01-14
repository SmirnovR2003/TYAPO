﻿using System.Collections.Generic;
using System.Data;
using System.Text;

namespace LLConverter_1
{
    public class FileParser(string fileName, bool directionSymbolsExistsInFile)
    {
        public List<GrammarRule> GrammarRules = [];

        private const char START_TOKEN_CH = '<';
        private const char END_TOKEN_CH = '>';
        private const int LINE_SEPARATION_LENGTH = 3;
        private const string EMPTY_SYMBOL = "e";
        private const string END_SYMBOL = "@";

        private readonly string[] _lines = ReadFile(fileName);
        private readonly bool _directionSymbolsExistsInFile = directionSymbolsExistsInFile;

        private List<string> _tokens = [];

        public static string[] ReadFile(string fileName)
        {
            var fileStream = File.OpenRead(fileName);
            List<string> result = [];
            string? line;
            using var reader = new StreamReader(fileStream);
            while ((line = reader.ReadLine()) != null)
            {
                result.Add(line);
            }
            return result.ToArray();
        }

        public void ParseLinesToGrammarRules()
        {
            ParseTokens();

            for (int i = 0; i < _lines.Length; i++)
            {
                GrammarRule grammarRule = new(_tokens[i], [], []);

                int startPos = _tokens[i].Length + 2 + LINE_SEPARATION_LENGTH;
                string line = _lines[i][startPos..];

                //if (_directionSymbolsExistsInFile)
                //{
                //    string[] arr = line.Split('/');
                //    if (arr.Length != 2)
                //    {
                //        throw new Exception("Wrong line format");
                //    }
                //    line = arr[0];
                //    grammarRule.DirectionSymbols = ParseDirectionSymbols(arr[1]);
                //}

                grammarRule.SymbolsChain = ParseChainSymbols(line);
                
                GrammarRules.Add(grammarRule);
            }
            GrammarRules.Insert(0, new GrammarRule("'" + GrammarRules[0].Token, [GrammarRules[0].Token, "@"], []));

            FixLeftRecursive();

            foreach (GrammarRule gr in GrammarRules)
            {
                Console.Write(gr.Token + '\t');
                foreach (var ch in gr.SymbolsChain)
                {
                    Console.Write(ch + ' ');
                }
                Console.WriteLine();
            }
            if (!_directionSymbolsExistsInFile)
            {
                FindDirectionSymbolsByRules();
            }
        }

        private void FixLeftRecursive()
        {
            List<GrammarRule> ruleыWithLeftRecursion = GrammarRules.FindAll(HasLeftRecursion);
            List<GrammarRule> rulesPassed = [];
            foreach (GrammarRule grammarRule in ruleыWithLeftRecursion)
            {
                RemoveLeftRecursion(grammarRule, rulesPassed);
                rulesPassed.Add(grammarRule);
            }
        }

        public void RemoveLeftRecursion(GrammarRule rule, List<GrammarRule> rulesPassed)
        {
            // Проверяем, есть ли левая рекурсия в правиле
            if (HasLeftRecursion(rule))
            {
                // Создаем новый нетерминал для замены леворекурсивных правил
                string newToken = rule.Token + "'";

                var rules = GrammarRules.FindAll(x => x.Token == rule.Token && !HasLeftRecursion(x));

                if(rules.Count == 0)
                {
                    throw new Exception("Can't remove left recursion");
                }

                /*B' -> aB'*/
                GrammarRule newRuleForRemoveLeftRecursion = new(newToken, new(rule.SymbolsChain.GetRange(1, rule.SymbolsChain.Count - 1)), new(rule.DirectionSymbols));
                newRuleForRemoveLeftRecursion.SymbolsChain.Add(newToken);

                GrammarRules[GrammarRules.IndexOf(rule)] = newRuleForRemoveLeftRecursion;

                if(rulesPassed.FindAll(x => x.Token == rule.Token).Count > 0)
                {
                    return;
                }

                GrammarRule ruleWithoutLeftRecursion = new (rules[0].Token, [], new(rule.DirectionSymbols));
                for (int i = 0; i < rules.Count; i++)
                {
                    ruleWithoutLeftRecursion = rules[i];

                    if (ruleWithoutLeftRecursion.SymbolsChain.Count == 0 )
                    {
                        continue;
                    }

                    GrammarRule newRule;
                    if (ruleWithoutLeftRecursion.SymbolsChain[0] == EMPTY_SYMBOL)
                    {
                        newRule = new(rule.Token, [], new(rule.DirectionSymbols));
                        newRule.SymbolsChain.AddRange(newRuleForRemoveLeftRecursion.SymbolsChain);
                        //newRule.SymbolsChain.Add(newToken);

                        if (rules.FindAll(x => x.SymbolsChain[0] != EMPTY_SYMBOL).Count == 0)
                        {
                            GrammarRules.Insert(GrammarRules.IndexOf(ruleWithoutLeftRecursion) + 1, newRule);
                        }

                        continue;
                    }

                    newRule = new(ruleWithoutLeftRecursion.Token, new(ruleWithoutLeftRecursion.SymbolsChain), new(rule.DirectionSymbols)); 
                    newRule.SymbolsChain.Add(newToken);

                    GrammarRules[GrammarRules.IndexOf(ruleWithoutLeftRecursion)] = newRule;
                }

                // Добавляем правила для обработки случая epsilon-продукции
                GrammarRule epsilonRule = new(newToken, ["e"], new(rule.DirectionSymbols));

                GrammarRules.Insert(GrammarRules.IndexOf(GrammarRules.FindLast(x => x.Token == newToken))+1, epsilonRule);
            }
        }

        private static bool HasLeftRecursion(GrammarRule rule)
        {
            return rule.SymbolsChain.Count > 0 && rule.SymbolsChain[0] == rule.Token;
        }

        /**
         * Поиск направляющих символов
         */
        private void FindDirectionSymbolsByRules()
        {
            List<int> listOfTokenIndexesWithEmptyChars = [];
            for (int index = 0; index < GrammarRules.Count; index++)
            {
                var grammarRule = GrammarRules[index];
                if (0 == grammarRule.DirectionSymbols.Count)
                {
                    grammarRule.DirectionSymbols.AddRange(FindDirectionSymbolsForToken(index));
                }
            }
            //List<int> listOfTokenIndexesWithEmptyChars = [];
            //for (int index = 0; index < GrammarRules.Count; index++)
            //{
            //    var grammarRule = GrammarRules[index];
            //    if (grammarRule.SymbolsChain.Contains(EMPTY_SYMBOL))
            //    {
            //        listOfTokenIndexesWithEmptyChars.Add(index);
            //    }
            //    else if (0 == grammarRule.DirectionSymbols.Count)
            //    {
            //        grammarRule.DirectionSymbols.AddRange(FindDirectionSymbolsForToken(index));
            //    }
            //}

            //foreach (int index in listOfTokenIndexesWithEmptyChars)
            //{
            //    FindDirectionSymbolsForEmptyChar(index);
            //}
        }

        private List<string> FindDirectionSymbolsForToken(int tokenIdx)
        {
            var grammarRule = GrammarRules[tokenIdx];
            var firstChainCharacter = grammarRule.SymbolsChain[0];

            if (TokenIsNonTerminal(firstChainCharacter))
            {
                List<string> result = [];
                for (int i = 0; i < GrammarRules.Count; i++)
                {
                    if (GrammarRules[i].Token == firstChainCharacter && i != tokenIdx)
                    {
                        result.AddRange(FindDirectionSymbolsForToken(i));
                    }
                }
                // Удаление дубликатов
                return result.Distinct().ToList();
            }
           
            
            return grammarRule.SymbolsChain.Contains(EMPTY_SYMBOL) ? Follow(grammarRule.Token).Distinct().ToList() : [firstChainCharacter];
        }

        List<string> Follow(string token)
        {
            List<string> dirSymbols = [];

            List<GrammarRule> grammarRules = GrammarRules.FindAll(x => x.SymbolsChain.Contains(token) && x.Token != token);

            for (int i = 0; i < grammarRules.Count; i++)
            {
                var grammarRule = grammarRules[i];

                int idx = grammarRule.SymbolsChain.IndexOf(token);

                if (idx == grammarRule.SymbolsChain.Count - 1 || ((idx == grammarRule.SymbolsChain.Count - 2) && (GrammarRules.IndexOf(grammarRule) == 0)))
                {
                    if (token != grammarRule.Token)
                    {
                        dirSymbols.AddRange(Follow(grammarRule.Token));
                        if ((idx == grammarRule.SymbolsChain.Count - 2) && (GrammarRules.IndexOf(grammarRule) == 0))
                            dirSymbols.Add("@");

                        continue;
                    }
                }
                //if((idx == grammarRule.SymbolsChain.Count - 2) && (GrammarRules.IndexOf(grammarRule) == 0))
                //{
                //    if (token != grammarRule.Token)
                //    {
                //        dirSymbols.AddRange(Follow(grammarRule.Token));
                //        continue;
                //    }

                //}
                if (idx != grammarRule.SymbolsChain.Count - 1)
                {
                    string symbol = grammarRule.SymbolsChain[idx + 1];
                    if (TokenIsNonTerminal(symbol))
                    {
                        List<GrammarRule> gramRules = GrammarRules.FindAll(x => x.Token == symbol && x.Token != grammarRule.Token);
                        for (int j = 0; j < gramRules.Count; j++)
                            dirSymbols.AddRange(FindDirectionSymbolsForToken(GrammarRules.IndexOf(gramRules[j])));
                    }
                    else if(symbol == EMPTY_SYMBOL)
                    {
                        dirSymbols.AddRange(Follow(grammarRule.Token));
                    }
                    else
                    {
                        dirSymbols.Add(symbol);
                    }
                }
            }

            return dirSymbols;
        }

        List<string> GetDirectionSymbolsByToken(string token)
        {
            //var token = GrammarRules[tokenIdx].Token;
            int idx = 0;
            List<string> result = [];
            foreach (GrammarRule grammarRule in GrammarRules)
            {
                if (token == grammarRule.Token)
                {
                    result.AddRange(FindDirectionSymbolsForToken(idx));
                }
                idx++;
            }
            return result;
        }

        bool TokenIsNonTerminal(string token)
        {
            foreach (GrammarRule grammarRule in GrammarRules)
            {
                if (grammarRule.Token == token)
                {
                    return true;
                }
            }
            return false;
        }

        private List<string> ParseChainSymbols(string str)
        {
            List<string> result = [];

            string accumulated = "";
            foreach (char ch in str)
            {
                if ((ch == ' ' || ch == START_TOKEN_CH) && accumulated.Length > 0)
                {
                    result.Add(accumulated);
                    accumulated = ch == START_TOKEN_CH ? ch.ToString() : "";
                }
                else if (ch == END_TOKEN_CH && accumulated.Length > 1 && _tokens.Contains(accumulated[1..]))
                {
                    result.Add(accumulated[1..]);
                    accumulated = "";
                }
                else if (ch != ' ')
                {
                    accumulated += ch;
                }
            }

            if(accumulated != "")
            {
                result.Add(accumulated);
            }

            return result;
        }

        private List<string> ParseDirectionSymbols(string str)
        {
            if (!str.Contains(','))
            {
                return [str.Trim()];
            }
            return new(str.Trim().Split(','));
        }

        /*
         * Находит на каждой строке левый нетерминал и добавляет в _tokens 
         */
        private void ParseTokens()
        {
            foreach (string line in _lines)
            {
                int tokenEndPos = line.IndexOf(END_TOKEN_CH);
                if (!line.StartsWith(START_TOKEN_CH) || tokenEndPos <= 1)
                {
                    throw new Exception("Wrong token format");
                }
                string token = line[1..tokenEndPos];
                _tokens.Add(token);
            }
        }

        // Вспомогательный вывод
        private void PrintGrammarRules()
        {
            Console.WriteLine("<------------------->");
            foreach (var rule in GrammarRules)
            {
                Console.Write(rule.Token + " -> ");
                foreach (var s in rule.SymbolsChain)
                {
                    Console.Write(s + "");
                }
                Console.Write(" / ");
                foreach (var s in rule.DirectionSymbols)
                {
                    Console.Write(s + "");
                }
                Console.WriteLine();
            }
            Console.WriteLine("<------------------->");
        }
    }
}
