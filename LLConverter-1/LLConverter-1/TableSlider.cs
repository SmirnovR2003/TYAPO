﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLConverter_1
{
    
    public class TableSlider
    {
        private readonly Lexer _lexer = new("lexer3.txt");
        public void RunSlider(Table table)
        {
            if (table == null) return;

            Stack<int> stack = new();
            int currRowNumber = 0;
            string currToken = _lexer.GetNextToken();
            Row currRow = table.Rows[currRowNumber];

            while (true)
            {
                if (currRow.End && stack.Count == 0 && _lexer.IsEnd()) break;

                if (currRow.DirectionSymbols.Contains(currToken))
                {
                    if (currRow.MoveToNextLine) stack.Push(currRowNumber + 1);
                    if (currRow.Pointer.HasValue)
                    {
                        currRowNumber = currRow.Pointer.Value;
                    }
                    else 
                    {
                        if (!stack.TryPop(out currRowNumber))
                        {
                            throw new Exception($"currRowNumber: {currRowNumber}, currToken: {currToken}, stack is empty");
                        }
                        
                    }

                    if (currRow.Shift) currToken = _lexer.GetNextToken();
                }
                else if (!currRow.Error)
                {
                    currRowNumber++;
                }
                else
                {
                    throw new Exception($"currRowNumber: {currRowNumber}, currToken: {currToken}, directionSymbols not contains currToken");
                }

                if (currRow.End && stack.Count == 0 && _lexer.IsEnd()) break;

                currRow = table.Rows[currRowNumber];
            }
        }
    }
    
}
