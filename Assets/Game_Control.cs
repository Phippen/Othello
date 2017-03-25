using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class Game_Control : MonoBehaviour {

    //Array that holds refrences to the chips themselves, used for displaying and animating them.
    static GameObject[,] boardSpaces = new GameObject[8, 8];

    //Array to represent the board state, 0 - no chip, 1 - player chip, 2 - AI chip
    static int[,] spaceOwner = new int[8, 8];

    //Used to keep track of number of valid flips in each direction (clockwise starting at top-left)
    static int[] flipCounts = new int[8];

    //Chip prefab
    public GameObject chip;
    //Record whos turn it is
    static bool playerTurn = true;
    //Decrement when placing a piece until 0
    private int placesLeft = 60;


    //Used to display end-game announcements as well as when someone has no moves, etc.
    public Text alert;

    //Display current scores(chip counts) during the game 
    public Text playerScoreText;
    public Text AIScoreText;

    //Allows player to adjust the game difficulty
    public Slider difficultySlider;
    public Text difficultyText;
    private int difficulty = 1;

    private int stall = 0;

    private bool gameOver;

    void Start() {

        //Initialize 4 starting pieces
        GameObject black1 = Instantiate(chip, new Vector3((float)(3.5), (float)(-3.5), (float)8.0), transform.rotation);
        GameObject black2 = Instantiate(chip, new Vector3((float)(4.5), (float)(-4.5), (float)8.0), transform.rotation);
        GameObject white1 = Instantiate(chip, new Vector3((float)(3.5), (float)(-4.5), (float)8.0), transform.rotation);
        GameObject white2 = Instantiate(chip, new Vector3((float)(4.5), (float)(-3.5), (float)8.0), transform.rotation);

        //Flip 2 to black(player)
        black1.transform.Rotate(new Vector3(180, 0, 0));
        black2.transform.Rotate(new Vector3(180, 0, 0));

        //Assign objects to array
        boardSpaces[3, 3] = black1;
        boardSpaces[4, 4] = black2;
        boardSpaces[3, 4] = white1;
        boardSpaces[4, 3] = white2;

        //Initialize state of board
        spaceOwner[3, 3] = 1;
        spaceOwner[4, 4] = 1;
        spaceOwner[3, 4] = 2;
        spaceOwner[4, 3] = 2;

        //Initialize animation asset
        DOTween.Init(false, true, LogBehaviour.ErrorsOnly);
    }

    void Update () {

        //Determine winner if there is one
        if(gameOver)
        {
            int[] scores = scoreBoard(spaceOwner, false);

            if (scores[0] > scores[1])
                alert.text = "You have won the game!";
            else if (scores[0] == scores[1])
                alert.text = "It's a draw!";
            else
                alert.text = "You have lost!";
            return;
        }
        else
        {
            //Check for game over
            if (placesLeft == 0)
            {
                gameOver = true;
                return;
            }
            //If current side has no possible moves..
            if (!hasMoves())
            {
                int[] scores = scoreBoard(spaceOwner, false);

                //Is it because someone has no chips on the board?
                if(scores[0] * scores[1] == 0)
                {
                    gameOver = true;
                }
                if(++stall >= 2)
                {
                    gameOver = true;
                }
                //Carry on to next player and use alert text to explain to user
                else
                {
                    String player = playerTurn ? "YOU" : "AI";
                    alert.text = player + " HAD NO MOVES!";
                    Invoke("resetAlertText", 2);
                    playerTurn = !playerTurn;
                }
                return;
            }
            stall = 0;
            
            //User's turn
            if (playerTurn)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Debug.Log("Player Turn");
                    Ray mouse = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit info;
                    bool hit = Physics.Raycast(mouse, out info, 500.0f);

                    if (hit)
                    {
                        int x = (int)Math.Floor(info.point.x);
                        int yPos = (int)Math.Ceiling(info.point.y);
                        int y = Math.Abs(yPos);

                        //If we clicked on a valid space
                        if (x >= 0 && x < 8 &&
                           yPos <= 0 && y < 8)
                        {
                            //If valid move
                            if (isMove(x, y, spaceOwner))
                            {
                                //Visually create piece and animate it down to board
                                GameObject newPiece = Instantiate(chip, new Vector3((float)(x + .5), (float)(yPos - .5), 0), transform.rotation);
                                newPiece.transform.Rotate(180, 0, 0); //Make black(user color)
                                newPiece.transform.DOMoveZ(8, (float).5, true);

                                //Update game state
                                boardSpaces[x, y] = newPiece;
                                spaceOwner[x, y] = 1;
                                placesLeft--;

                                //Do all flips that occured from move
                                findFlipDirections(x, y, spaceOwner, true);
                                playerTurn = !playerTurn;
                            }
                            else
                            {
                                Debug.Log("That was not a valid move.");
                            }

                        }
                    }
                    updateScore();
                }
            }
            else
            {
                Debug.Log("AI Turn");
                AI();
                updateScore();
            }
        }
    }

    public void checkDifficulty()
    {
        difficulty = (int)difficultySlider.value;
        difficultyText.text = "Difficulty : " + difficulty;
    }


    private bool hasMoves()
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (isMove(i, j, spaceOwner))
                {
                    return true;
                }
            }
        }
        return false;
    }

    //By summing the 1's we have player score and summing 2's we get AI score, using this to avoid incrementing and decrementing bugs when placing/flipping.
    private void updateScore()
    {
        int[] currentScores = scoreBoard(spaceOwner, false);
        playerScoreText.text = "Player Score : " + currentScores[0];
        AIScoreText.text = "AI Score : " + currentScores[1];
    }




    void AI()
    {
        //Array for negamax to reference
        int[] nextMove = new int[2] { -1, -1 };
        negaMax(spaceOwner, difficulty, ref nextMove);

        //Reset this right away
        playerTurn = false;
        if (nextMove[0] >= 0 && nextMove[1] >= 0)
        {
            int x = nextMove[0];
            int y = nextMove[1];
            checkFlips(x, y, spaceOwner);
            GameObject newPiece = Instantiate(chip, new Vector3((float)(x + .5), (float)(-y - .5), 0), transform.rotation);
            newPiece.transform.DOMoveZ(8, (float)0.5, true);
            boardSpaces[x, y] = newPiece;
            spaceOwner[x, y] = 2;
            placesLeft--;
            findFlipDirections(x, y, spaceOwner, true);
        }
        else
        {
            Debug.Log("Error: A best move was not found.");
        }
        playerTurn = true;
    }

    //Used for debugging
    private static void DebugBoard()
    {
        Debug.Log("[" + spaceOwner[0, 0] + "," + spaceOwner[1, 0] + "," + spaceOwner[2, 0] + "," + spaceOwner[3, 0] + "," + spaceOwner[4, 0] + "," + spaceOwner[5, 0] + "," + spaceOwner[6, 0] + "," + spaceOwner[7, 0] + "]\n" +
                    "[" + spaceOwner[0, 1] + "," + spaceOwner[1, 1] + "," + spaceOwner[2, 1] + "," + spaceOwner[3, 1] + "," + spaceOwner[4, 1] + "," + spaceOwner[5, 1] + "," + spaceOwner[6, 1] + "," + spaceOwner[7, 1] + "]\n" +
                    "[" + spaceOwner[0, 2] + "," + spaceOwner[1, 2] + "," + spaceOwner[2, 2] + "," + spaceOwner[3, 2] + "," + spaceOwner[4, 2] + "," + spaceOwner[5, 2] + "," + spaceOwner[6, 2] + "," + spaceOwner[7, 2] + "]\n" +
                    "[" + spaceOwner[0, 3] + "," + spaceOwner[1, 3] + "," + spaceOwner[2, 3] + "," + spaceOwner[3, 3] + "," + spaceOwner[4, 3] + "," + spaceOwner[5, 3] + "," + spaceOwner[6, 3] + "," + spaceOwner[7, 3] + "]\n" +
                    "[" + spaceOwner[0, 4] + "," + spaceOwner[1, 4] + "," + spaceOwner[2, 4] + "," + spaceOwner[3, 4] + "," + spaceOwner[4, 4] + "," + spaceOwner[5, 4] + "," + spaceOwner[6, 4] + "," + spaceOwner[7, 4] + "]\n" +
                    "[" + spaceOwner[0, 5] + "," + spaceOwner[1, 5] + "," + spaceOwner[2, 5] + "," + spaceOwner[3, 5] + "," + spaceOwner[4, 5] + "," + spaceOwner[5, 5] + "," + spaceOwner[6, 5] + "," + spaceOwner[7, 5] + "]\n" +
                    "[" + spaceOwner[0, 6] + "," + spaceOwner[1, 6] + "," + spaceOwner[2, 6] + "," + spaceOwner[3, 6] + "," + spaceOwner[4, 6] + "," + spaceOwner[5, 6] + "," + spaceOwner[6, 6] + "," + spaceOwner[7, 6] + "]\n" +
                    "[" + spaceOwner[0, 7] + "," + spaceOwner[1, 7] + "," + spaceOwner[2, 7] + "," + spaceOwner[3, 7] + "," + spaceOwner[4, 7] + "," + spaceOwner[5, 7] + "," + spaceOwner[6, 7] + "," + spaceOwner[7, 7] + "]\n" );
    }
    /*
     * @return int[] | { playerScore , AIScore }
     * @param board | Board to score
     * @param bias | Should hueristic or traditional scoring be used?
     */
    private int[] scoreBoard(int[,] board, bool hueristic)
    {
        int newPlayerScore = 0;
        int newAIScore = 0;

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                //Do I like ternarys too much? Likely.. If we are scoring for negamax, give corners and sides weight, otherwise we want actual chip count score for UI, so just count chips.
                if (board[i, j] == 1)
                {
                    int scoreBias = hueristic ? (isCorner(i, j) ? 7 : isSide(i, j) ? 3 : 1) : 1;
                    newPlayerScore += scoreBias;
                }
                else if (board[i, j] == 2)
                {
                    int scoreBias = hueristic ? (isCorner(i, j) ? 7 : isSide(i, j) ? 3 : 1) : 1;
                    newAIScore += scoreBias;
                }
            }
        }
        return new int[2] { newPlayerScore, newAIScore };
    }

    
    //Just brute force it
    private bool isCorner(int i, int j)
    {
        return (i == 0 && j == 0) || (i == 0 && j == 7) || (i == 7 && j == 0) || (i == 7 && j == 7);
    }

    private bool isSide(int i, int j)
    {
        return i == 7 || i == 0 || j == 0 || j == 7;
    }

    /*
     * @return int | Heuristic score of board for recursive analysis.
     * 
     * @param board | Board to apply theoretical moves to and score.
     * @param depth | How much fire my poor laptop produces out the vents.
     * @param myBestMove | reference for top-level call to output the move the AI should make, other calls bestMoves could probably be used for fancy tree pruning.
     */
    private int negaMax(int [,] board, int depth, ref int[] myBestMove)
    {
        double bestScore = Double.NegativeInfinity;

        //No more thinking, score the board
        if (depth == 0)
        {
            int[] scores = scoreBoard(board, true);
            //playerScore - AIScore, since this is being returned to be negated already, it will become AI advantage so leave it if we are returning to AI perspective.
            int pAdvantage = scores[0] - scores[1];

            return pAdvantage * (playerTurn ? -1 : 1);
        }
        //Foreach possible move..
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (isMove(i, j, board))
                {
                    //"Make" the move on a fake board
                    int[,] newBoard = (int[,])board.Clone();
                    newBoard[i, j] = playerTurn ? 1 : 2;

                    //Alter our new board accordingly
                    findFlipDirections(i, j, newBoard, false);


                    int[] childBestMove = new int[2];
                    playerTurn = !playerTurn;
                    int score = -negaMax(newBoard, depth - 1, ref childBestMove);
                    //If this move path is better than previous best..
                    if(score > bestScore)
                    {
                        //Update score for further processing and then store the move we made, latter only matters for top-level as is.
                        bestScore = score;
                        myBestMove = new int[2] { i, j };
                    }
                }
            }
        }
        return (int)bestScore;
    }


    //Check that chosen space doesn't have a chip already and that there are chips to flip with this move.
    bool isMove(int x, int y, int[,] board)
    {
        if (board[x, y] != 0)
        {
            return false;
        }

        checkFlips(x, y, board);
        return findValidMove();
    }

    private bool findValidMove()
    {
        bool result = false;

        for(int i = 0; i < flipCounts.Length; i++)
        {
            result |= flipCounts[i] > 0;
        }
        
        return result;
    }

    //Populate flipCount array for validation as well as for flipping chips later.
    void checkFlips(int x, int y, int[,] board)
    {
        flipCounts = new int[8];
        int count = 0;

        if(countFlips(x, y, -1, -1, ref count, board))
        {
            flipCounts[0] = count;
        }
        count = 0;

        if(countFlips(x, y, 0, -1, ref count, board))
        {
            flipCounts[1] = count;
        }
        count = 0;

        if(countFlips(x, y, 1, -1, ref count, board))
        {
            flipCounts[2] = count;
        }
        count = 0;

        if(countFlips(x, y, 1, 0, ref count, board))
        {
            flipCounts[3] = count;
        }
        count = 0;

        if(countFlips(x, y, 1, 1, ref count, board))
        {
            flipCounts[4] = count;
        }
        count = 0;

        if(countFlips(x, y, 0, 1, ref count, board))
        {
            flipCounts[5] = count;
        }
        count = 0;

        if(countFlips(x, y, -1, 1, ref count, board))
        {
            flipCounts[6] = count;
        }
        count = 0;

        if(countFlips(x, y, -1, 0, ref count, board))
        {
            flipCounts[7] = count;
        }
    }

    //Count number of possible flips in a particular direction recursively and return true when we reach an allied piece.
    bool countFlips(int startX, int startY, int xModify, int yModify, ref int count, int [,] board)
    {
        int currentX = startX + xModify;
        int currentY = startY + yModify;

        if (currentX > 7 || currentX < 0 ||
            currentY > 7 || currentY < 0)
        {
            return false;
        }

        //If there's a piece here..

        if (board[currentX, currentY] != 0)
        {
            //Is it an "my" piece?
            if (isMyPiece(currentX, currentY, board))
            {
                //Return true if this piece isn't directly next to where we started.
                return count > 0;
            }
            else
            {
                //Keep going
                count++;
                return countFlips(currentX, currentY, xModify, yModify, ref count, board);
            }
        }
        else
            return false;
    }

    private bool isMyPiece(int x, int y, int[,] board)
    {
        return playerTurn ? board[x, y] == 1 : board[x, y] == 2;
    }

    //For each direction we had flips, call flipPieces in that direction
    void findFlipDirections(int x, int y, int[,] board, bool realMove)
    {                
        if(flipCounts[0] > 0)
        {
            flipPieces(x, y, -1, -1, board, realMove);
        }
        if (flipCounts[1] > 0)
        {
            flipPieces(x, y, 0, -1, board, realMove);
        }
        if (flipCounts[2] > 0)
        {
            flipPieces(x, y, 1, -1, board, realMove);
        }
        if (flipCounts[3] > 0)
        {
            flipPieces(x, y, 1, 0, board, realMove);
        }
        if (flipCounts[4] > 0)
        {
            flipPieces(x, y, 1, 1, board, realMove);
        }
        if (flipCounts[5] > 0)
        {
            flipPieces(x, y, 0, 1, board, realMove);
        }
        if (flipCounts[6] > 0)
        {
            flipPieces(x, y, -1, 1, board, realMove);
        }
        if (flipCounts[7] > 0)
        {
            flipPieces(x, y, -1, 0, board, realMove);
        }
    }

    /*
     * Recursively flip pieces until allied piece is reached.
     * 
     * @param realMove | true if we need to involve the actual GameObjects and animate them, false if negamax planning
     */
    void flipPieces(int startX, int startY, int xModify, int yModify, int[,] board, bool realMove)
    {

        
        int currentX = startX + xModify;
        int currentY = startY + yModify;

        //Is it an "my" piece?
        if (isMyPiece(currentX, currentY, board))
        {
            //Done
            return;
        }
        else
        {
            //Rotate piece visually if we're actually making a move
            if (realMove)
            {
                int targetRotation = playerTurn ? 90 : -90;
                boardSpaces[currentX, currentY].transform.DORotate(new Vector3(targetRotation, 0, 0), 1);
            }

            //Change owner
            board[currentX, currentY] = playerTurn ? 1 : 2;
            
            //Keep going
            flipPieces(currentX, currentY, xModify, yModify, board, realMove);
        }
    }

    void resetAlertText()
    {
        alert.text = "";
    }
}