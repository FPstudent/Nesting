﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Nesting_3
{
    /// <summary>
    /// classe che implementa l'euristica 
    /// </summary>
    class HSolve : IHSolve
    {
        /// <summary>
        /// la configurazione contenwnte i parametri con 
        /// cui lanciare l'algortimo hsolve
        /// </summary>
        public Configuration Configuration { get; set; } = null;

        /// <summary>
        /// ogni container appartente a containers contiene
        /// gli n bin di una certa iterazione
        /// </summary>
        public IList<Sequence> Sequences { get; set; } = null;

        /// <summary>
        /// costruttore in cui setto la configuration di hsolve
        /// </summary>
        /// <param name="configuration"></param>
        public HSolve(Configuration configuration)
        {
            Configuration = configuration;
            Sequences = new List<Sequence>();
        }

        /// <summary>
        /// metodo che computa l'euristica di hsolve 
        /// </summary>
        public IList<Sequence> ComputeHeuristic()
        {
            IUtilities utilities = new Utilities();

            //================ STEP 1 - INITIALIZATION ================

            //inizializzo il prezzo v associato ad ogni item j
            IList<PricedItem> pricedItems = new List<PricedItem>();
            int counter = 0;

            foreach (var dimension in Configuration.Dimensions) { 
                var pricedItem = new PricedItem()
                {
                    Height = dimension.Height,
                    Width = dimension.Width,
                    Id = counter,
                    Price = dimension.Height * dimension.Width,
                };
                pricedItems.Add(pricedItem);
                counter += 1;
            }

            IList<Bin<Tuple>> bins = new List<Bin<Tuple>>();
            counter = 0;

            //inserisco ogni item prezzato e i nuovi punti disponibili 
            //in un bin diverso
            foreach(var pricedItem in pricedItems)
            {
                var bin = new Bin<Tuple>()
                {
                    Id = counter,
                    Height = Configuration.BinHeight,
                    Width = Configuration.BinWidth,
                    PricedItems = new List<PricedItem>()
                    {
                        pricedItems.ElementAt(counter)
                    },
                    Points = new List<Tuple>()
                    {
                        new Tuple()
                        {
                            Pposition = 0,
                            Qposition = pricedItem.Height,
                            IsUsed = false
                        },
                        new Tuple()
                        {
                            Pposition = pricedItem.Width,
                            Qposition = 0,
                            IsUsed = false
                        }
                    }
                };

                bins.Add(bin);
                counter += 1;
            }

            int itemNumber = counter + 1;
            counter = 0;
            //inizializzo il costo della soluzione con il numero degli elementi
            int zStar = itemNumber;

            //inizializzo il numero di iterazioni
            int iter = 0;

            //calcolo il lower bound ed il relativo intervallo
            float lowerBound = utilities.ComputeLowerBound(pricedItems, Configuration.BinWidth, Configuration.BinHeight);
            float lowerBoundMin = lowerBound - Configuration.LowerBoundDelta;
            float lowerBoundMax = lowerBound + Configuration.LowerBoundDelta;
            int maxIter = Configuration.MaxIter;

        //================ STEP 2 - ERASE THE CURRENT SOLUTION ================

        l3: //creo una lista temporanea J' di item 
            IList<PricedItem> temporaryPricedItems = new List<PricedItem>();

            //assegno la lista di item J a J'
            temporaryPricedItems = pricedItems;

            //setto il bin che considero al momento
            int i = 0;

            //creo tanti bin temporanei quanti sono gli item
            IList<Bin<Tuple>> temporaryBins = new List<Bin<Tuple>>();
            foreach (var pricedItem in pricedItems)
            {
                var temporaryBin = new Bin<Tuple>()
                {
                    Id = counter,
                    Height = Configuration.BinHeight,
                    Width = Configuration.BinWidth,
                    PricedItems = null,
                    Points = new List<Tuple>()
                    {
                        new Tuple()
                        {
                            Pposition = 0,
                            Qposition = 0,
                            IsUsed = false
                        }
                    }
                };
                temporaryBins.Add(temporaryBin);
                counter += 1;
            }

            //ogni volta che inizio una nuova iterazione iter devo riordinare gli item per price decrescente
            //dato che i price sono stati aggiornati
            var sortedTemporaryPricedItems = temporaryPricedItems.OrderByDescending(x => x.Price);

        //================ STEP 3 - FILLING UP BIN i ================
        l1://cerco la posizione migliore per ogni item j'
            foreach (var sortedTemporaryPricedItem in sortedTemporaryPricedItems)
            {
                if (!sortedTemporaryPricedItem.IsRemoved)
                {
                    utilities.IsBestPositionFound(temporaryBins.ElementAt(i), sortedTemporaryPricedItem);
                }
            }

            //================ STEP 4 - CHECK IF ALL ITEMS HAVE BEEN ALLOCATED ================
            int z = i;
            bool isSortedTemporaryPricedItemsEmpty = true;

            //controllo se tutta la lista è stata svuotata
            foreach (var sortedTemporaryPricedItem in sortedTemporaryPricedItems)
            {
                if (sortedTemporaryPricedItem.IsRemoved == false)
                {
                    isSortedTemporaryPricedItemsEmpty = false;
                    break;
                }
            }

            if (isSortedTemporaryPricedItemsEmpty)
            {
                goto l0;
            }
            if (!isSortedTemporaryPricedItemsEmpty && (i < (zStar - 1)))
            {
                i += 1;
                goto l1;
            }
            goto l2;

        //================ STEP 5 - UPDATE THE BEST SOLUTION ================

        l0: zStar = z;
            bins = temporaryBins;

            //================ STEP 6 - CHECK OPTIMALITY ================
            //guardo se il costo della soluzione è compreso nell'intervallo del lower bound 
            if (zStar > lowerBoundMin && zStar < lowerBoundMax)
            {
                goto end;
            }

        //================ STEP 7 - UPDATE THE ITEM PRICES ================

        l2: if (iter == maxIter)
            {
                goto end;
            }
            else
            {
                utilities.UpdatePrice(z, pricedItems, bins);

                //creo un nuovo containter con tutti i bin dell'iterazione corrente
                Sequence sequence = new Sequence()
                {
                    Bins = bins
                };

                //aggiungo il container di una certa iterazione iter alla lista dei containers
                Sequences.Add(sequence);

                iter += 1;
                //rimetto tutti gli item come isRemoved = false perché cominicio una nuova iterazione
                foreach (var pricedItem in pricedItems)
                {
                    pricedItem.IsRemoved = false;
                }
                goto l3;
            }

        end:
            //creo un nuovo containter con tutti i bin dell'iterazione corrente
            Sequence sequence1 = new Sequence()
            {
                Bins = bins
            };

            //aggiungo il container di una certa iterazione iter alla lista dei containers
            Sequences.Add(sequence1);
            return Sequences;
        }
    }
}
