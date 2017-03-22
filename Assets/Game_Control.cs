using System;
using UnityEngine;



public class Game_Control : MonoBehaviour {

    //X,Y
    static GameObject[,] boardSpaces = new GameObject[8, 8];

    //0 means no chip, 1 means player(black) chip, 2 means AI(white) chip
    static int[,] spaceOwner = new int[8, 8];
    
    //Used to keep track of flip counts in each direction (clockwise starting at top-left)
    static int[] flipCounts = new int[8];
    //Prefab
    public GameObject chip;
    //Keep track of turn
    static bool playerTurn = true;

    void Start() {

        //Initialize 4 starting pieces
        GameObject black1 = Instantiate(chip, new Vector3((float)(3.5), (float)(-3.5), (float)8.0), transform.rotation);
        GameObject black2 = Instantiate(chip, new Vector3((float)(4.5), (float)(-4.5), (float)8.0), transform.rotation);
        GameObject white1 = Instantiate(chip, new Vector3((float)(3.5), (float)(-4.5), (float)8.0), transform.rotation);
        GameObject white2 = Instantiate(chip, new Vector3((float)(4.5), (float)(-3.5), (float)8.0), transform.rotation);

        //Flip 2 to black
        black1.transform.Rotate(new Vector3(180, 0, 0));
        black2.transform.Rotate(new Vector3(180, 0, 0));

        //Put
        boardSpaces[3, 3] = black1;
        boardSpaces[4, 4] = black2;
        boardSpaces[3, 4] = white1;
        boardSpaces[4, 3] = white2;

        spaceOwner[3, 3] = 1;
        spaceOwner[4, 4] = 1;
        spaceOwner[3, 4] = 2;
        spaceOwner[4, 3] = 2;
    }

    void Update () {
        if(playerTurn)
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
                        if (isMove(x, y))
                        {
                            //Place piece
                            GameObject newPiece = Instantiate(chip, new Vector3((float)(x + .5), (float)(yPos - .5), (float)8.0), transform.rotation);
                            newPiece.transform.Rotate(180, 0, 0);
                            Debug.Log("Placed at " + x + "," + y);
                            boardSpaces[x, y] = newPiece;
                            spaceOwner[x, y] = 1;

                            //Flip
                            findFlipDirections(x,y);
                            playerTurn = !playerTurn;
                        }

                    }
                }
            }
        }
        else
        {
            Debug.Log("AI Turn");
            AI();
        }
        flipCounts = new int[] {0,0,0,0,0,0,0,0};

    }

    void AI()
    {
        for(int i = 0; i < 8; i++)
        {
            for(int j = 0; j < 8; j++)
            {
                if(isMove(i,j))
                {
                    GameObject newPiece = Instantiate(chip, new Vector3((float)(i + .5), (float)(-j - .5), (float)8.0), transform.rotation);
                    boardSpaces[i, j] = newPiece;
                    spaceOwner[i, j] = 2;
                    findFlipDirections(i, j);
                    playerTurn = !playerTurn;
                    return;
                }
            }
        }
        Debug.Log("No Moves");
        playerTurn = !playerTurn;
    }

    //Is there a space around chosen point that is a chip and not our color?
    bool isMove(int x, int y)
    {
        if (boardSpaces[x, y])
        {
            return false;
        }

        checkFlips(x, y);
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

    //Populate flipCount array for validation as well as future flipping.
    void checkFlips(int x, int y)
    {
        int count = 0;
        if(countFlips(x, y, -1, -1, ref count))
        {
            flipCounts[0] = count;
        }
        count = 0;

        if(countFlips(x, y, 0, -1, ref count))
        {
            flipCounts[1] = count;
        }
        count = 0;

        if(countFlips(x, y, 1, -1, ref count))
        {
            flipCounts[2] = count;
        }
        count = 0;

        if(countFlips(x, y, 1, 0, ref count))
        {
            flipCounts[3] = count;
        }
        count = 0;

        if(countFlips(x, y, 1, 1, ref count))
        {
            flipCounts[4] = count;
        }
        count = 0;

        if(countFlips(x, y, 0, 1, ref count))
        {
            flipCounts[5] = count;
        }
        count = 0;

        if(countFlips(x, y, -1, 1, ref count))
        {
            flipCounts[6] = count;
        }
        count = 0;

        if(countFlips(x, y, -1, 0, ref count))
        {
            flipCounts[7] = count;
        }
    }

    //Count number of possible flips in a particular direction recursively and return true when we reach an allied piece.
    bool countFlips(int startX, int startY, int xModify, int yModify, ref int count)
    {
        int currentX = startX + xModify;
        int currentY = startY + yModify;

        if (currentX > 7 || currentX < 0 ||
            currentY > 7 || currentY < 0)
        {
            return false;
        }

        //If there's a piece here..

        if (boardSpaces[currentX, currentY])
        {

            //Is it an "my" piece?
            if (isMyPiece(currentX, currentY))
            {
                //Return true if this piece isn't directly next to where we started.
                Debug.Log("Piece at " + currentX + "," + currentY + " is mine! Count is " + count);
                return count > 0;
            }
            else
            {
                Debug.Log("Piece at " + currentX + "," + currentY + " is an enemy!");
                //Keep going
                count++;
                return countFlips(currentX, currentY, xModify, yModify, ref count);
            }
        }
        else
            return false;
    }

    private bool isMyPiece(int x, int y)
    {
        return playerTurn ? spaceOwner[x, y] == 1 : spaceOwner[x, y] == 2;
    }

    void findFlipDirections(int x, int y)
    {
        for(int i = 0; i < flipCounts.Length; i++)
        {
            if(flipCounts[i] > 0) Debug.Log("Count at index " + i + " is " + flipCounts[i]);
        }
                
        if(flipCounts[0] > 0)
        {
            flipPieces(x, y, -1, -1);
        }
        if (flipCounts[1] > 0)
        {
            flipPieces(x, y, 0, -1);
        }
        if (flipCounts[2] > 0)
        {
            flipPieces(x, y, 1, -1);
        }
        if (flipCounts[3] > 0)
        {
            flipPieces(x, y, 1, 0);
        }
        if (flipCounts[4] > 0)
        {
            flipPieces(x, y, 1, 1);
        }
        if (flipCounts[5] > 0)
        {
            flipPieces(x, y, 0, 1);
        }
        if (flipCounts[6] > 0)
        {
            flipPieces(x, y, -1, 1);
        }
        if (flipCounts[7] > 0)
        {
            flipPieces(x, y, -1, 0);
        }
    }

    //Recursively flip pieces until allied piece is reached.
    void flipPieces(int startX, int startY, int xModify, int yModify)
    {
        int currentX = startX + xModify;
        int currentY = startY + yModify;

        //Is it an "my" piece?
        if (isMyPiece(currentX, currentY))
        {
            //Done
            return;
        }
        else
        {
            //Rotate piece visually
            boardSpaces[currentX, currentY].transform.Rotate(new Vector3(180, 0, 0));

            //Change owner
            spaceOwner[currentX, currentY] = playerTurn ? 1 : 2;

            Debug.Log("Flipped piece at: " + currentX + "," + currentY);
            //Keep going
            flipPieces(currentX, currentY, xModify, yModify);
        }
    }
}