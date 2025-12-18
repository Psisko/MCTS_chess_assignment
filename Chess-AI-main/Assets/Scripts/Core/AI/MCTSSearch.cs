namespace Chess
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Design.Serialization;
    using System.Threading;
    using Unity.PlasticSCM.Editor.WebApi;
    using UnityEngine;
    using static System.Math;
    class MCTSSearch : ISearch
    {
        public event System.Action<Move> onSearchComplete;

        MoveGenerator moveGenerator;

        Move bestMove;
        int bestEval;
        bool abortSearch;

        MCTSSettings settings;
        Board board;
        Evaluation evaluation;

        System.Random rand;

        // Diagnostics
        public SearchDiagnostics Diagnostics { get; set; }
        System.Diagnostics.Stopwatch searchStopwatch;

        public MCTSSearch(Board board, MCTSSettings settings)
        {
            this.board = board;
            this.settings = settings;
            evaluation = new Evaluation();
            moveGenerator = new MoveGenerator();
            rand = new System.Random();
        }

        public void StartSearch()
        {
            InitDebugInfo();

            // Initialize search settings
            bestEval = 0;
            bestMove = Move.InvalidMove;

            moveGenerator.promotionsToGenerate = settings.promotionsToSearch;
            abortSearch = false;
            Diagnostics = new SearchDiagnostics();

            SearchMoves();

            onSearchComplete?.Invoke(bestMove);

            if (!settings.useThreading)
            {
                LogDebugInfo();
            }
        }

        public void EndSearch()
        {
            if (settings.useTimeLimit)
            {
                abortSearch = true;
            }
        }

        void SearchMoves()
        {
            MCTSNode rootNode = new(board.Clone(), Move.InvalidMove);

            for (int i = 0; i < settings.maxNumOfPlayouts; i++)
            {
                MCTSNode currentBestNode = rootNode;
                if (abortSearch) 
                    break;

                // Selection
                while(currentBestNode.isExpandable())
                {
                    currentBestNode = currentBestNode.Selection();
                }

                // Expansion
                if (!currentBestNode.hasEnded())
                    currentBestNode.Expand();

                // Simulation
                double simulationResult = currentBestNode.Simulate();

                // Backpropagation
                currentBestNode.PropagateResult(simulationResult);
            }

            onSearchComplete?.Invoke(rootNode.Selection().movePlayed);
            

            // throw new NotImplementedException();
        }

        void LogDebugInfo()
        {
            // Optional
        }

        void InitDebugInfo()
        {
            searchStopwatch = System.Diagnostics.Stopwatch.StartNew();
            // Optional
        }
    }
}