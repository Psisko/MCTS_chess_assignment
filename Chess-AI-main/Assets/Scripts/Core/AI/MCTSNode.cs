namespace Chess
{
    using System;
    using System.Collections.Generic;
    using Unity.PlasticSCM.Editor.WebApi;
    using Unity.VisualScripting;
    using UnityEditor.Hardware;
    using UnityEngine;
    using UnityEngine.UIElements;
    using static UnityEngine.ParticleSystem;
    using static UnityEngine.UIElements.UxmlAttributeDescription;


    public class MCTSNode
    {
        public Board boardState;
        public HashSet<MCTSNode> children;
        public MCTSNode parent;
        public Move movePlayed;

        public int visits;
        public int unvisitedChildren;
        public double positionEvaluation;
        public double UCBValue = double.PositiveInfinity;


        public MCTSNode(Board b, Move mp, MCTSNode p = null)
        {
            this.visits = 0;
            this.parent = p;
            this.movePlayed = mp;
            this.boardState = b;
            this.unvisitedChildren = 0;
            this.positionEvaluation = 0;
        }
    
        public MCTSNode Selection()
        {
            MCTSNode current = this;

            // Traverse the tree until a leaf node is reached
            while (current.children.Count > 0)
            {
                MCTSNode bestNode = null;
                double bestUCB = double.NegativeInfinity;

                foreach (MCTSNode child in current.children)
                {
                    // Select the child with the highest UCB value
                    if (child.UCBValue > bestUCB)
                    {
                        bestUCB = child.UCBValue;
                        bestNode = child;
                    }
                }

                current = bestNode;
            }

            return current; // Return the leaf node
        }

        public void Expand()
        {
            // Generate all possible moves for the current board state
            List<Move> possibleMoves;
            MoveGenerator moveGenerator = new MoveGenerator();

            // If this is the root node, generate moves for the current player
            if (parent == null)
            {
                possibleMoves = moveGenerator.GenerateMoves(boardState, true);
            }
            else
            {
                possibleMoves = moveGenerator.GenerateMoves(boardState, false);
            }

            unvisitedChildren = possibleMoves.Count;

            // Initialize variables to track the best node
            MCTSNode bestNode = null;
            double bestUCB = double.NegativeInfinity;

            for(int index = possibleMoves.Count - 1; index >= 0; index--)
            {
                var move = possibleMoves[index];
                // Clone the board state and apply the move
                Board clonedBoard = boardState.Clone();
                clonedBoard.MakeMove(move);

                // Create a new child node and add it to the children
                MCTSNode childNode = new MCTSNode(clonedBoard, move, this);
                childNode.CalculateUCB();

                unvisitedChildren--;
                children.Add(childNode);

                if (childNode.UCBValue > bestUCB)
                {
                    bestUCB = childNode.UCBValue;
                    bestNode = childNode;
                }
            }
        }

        //When performing simulations, first clone the state in the
        //node using the Board.GetLightWeightClone method and use the lightweight clone.

        //When evaluating states, use rewards 0 = loss, 1 = win,
        //Evaluation.EvaluateSimBoard() for anything else.
        //(Alternatively, you can use different rewards if you find it easier,
        //but you need to adjust UCB accordingly in order to achieve the same results.)

        public double Simulate()
        {
            Board simulationBoard = boardState.Clone();
            MoveGenerator moveGenerator = new MoveGenerator();
            List<Move> possibleMoves = moveGenerator.GenerateMoves(simulationBoard, false);

            int currentDepth = 0;
            int maxSimulationDepth = 6;

            // Stops if the game has ended or max depth reached
            while (possibleMoves.Count > 0 && currentDepth < maxSimulationDepth)
            {
                // Pick a random move
                int randomIndex = UnityEngine.Random.Range(0, possibleMoves.Count);
                Move randomMove = possibleMoves[randomIndex];

                // Apply move
                simulationBoard.MakeMove(randomMove);
                currentDepth++;

                possibleMoves = moveGenerator.GenerateMoves(simulationBoard, false);
            }

            Evaluation evaluation = new Evaluation();


            bool evaluationPerspective = (simulationBoard.OpponentColour == Board.WhiteIndex);

            double score = evaluation.EvaluateSimBoard(simulationBoard.GetLightweightClone(), evaluationPerspective);

            return score;
        }

        public void PropagateResult(double evaluation)
        {
            visits++;
            positionEvaluation += evaluation;

            if (parent != null)
            {
                // 1.0 - evaluation to switch perspectives for each parent node
                parent.PropagateResult(1.0 - evaluation);
            }
        }

        public bool isExpandable()
        {
            return unvisitedChildren == 0;
        }

        public bool hasEnded()
        {
            // when one of the kings is captured
            return false;
        }

        public void CalculateUCB(double explorationConstant = 1.0)
        {
            if (visits == 0)
            {
                UCBValue = double.MaxValue;
                return;
            }

            double averageEvaluation = (double)positionEvaluation / visits; // Q
            double explorationTerm = explorationConstant * Math.Sqrt(Math.Log(parent.visits) / (visits));
            UCBValue = averageEvaluation + explorationTerm;
        }


    }
}