using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance {get; private set;}

    public event EventHandler<OnClickedOnGridPositionEventArgs> OnClickedOnGridPosition;
    public class OnClickedOnGridPositionEventArgs : EventArgs {
        public int x;
        public int y;
        public PlayerType playerType;
    }

    public event EventHandler OnGameStarted;
    public event EventHandler<OnGameWinEventArgs> OnGameWin;
    public class OnGameWinEventArgs : EventArgs{
        public Line line;
        public PlayerType winPlayerType;
    }
    public event EventHandler OnCurrentPlayablePlayerTypeChanged;
    public event EventHandler OnRematch;
    public event EventHandler OnGameTied;
    public event EventHandler OnPlacedObject;
    public enum PlayerType {
        None,
        Cross,
        Circle,
    }

    public enum Orientation {
        Horizontal,
        Vertical,
        DiagonalA,
        DiagonalB,
    }
    public struct Line {
        public List<Vector2Int> gridVector2IntList;
        public Vector2Int centerGridPosition;
        public Orientation orientation;
    }

    private PlayerType localPlayerType;

    //Synchronizes across all networks
    private NetworkVariable<PlayerType> currentPlayablePlayerType = new NetworkVariable<PlayerType>();
    public PlayerType[,] playerTypeArray;

    private List<Line> lineList;
    private void Awake()
    {
        if(Instance != null){
            Debug.LogError("More than one GameManager instance");
        }
        Instance = this;

        playerTypeArray = new PlayerType[3, 3];

        lineList = new List<Line> {
            //Horizontal Win Lines
            new Line {
            gridVector2IntList = new List<Vector2Int>{ new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), },
                centerGridPosition = new Vector2Int(1,0),
                orientation = Orientation.Horizontal,
            },
            new Line {
            gridVector2IntList = new List<Vector2Int>{ new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1), },
                centerGridPosition = new Vector2Int(1,1),
                orientation = Orientation.Horizontal,
            },
            new Line {
            gridVector2IntList = new List<Vector2Int>{ new Vector2Int(0,2), new Vector2Int(1,2), new Vector2Int(2,2), },
                centerGridPosition = new Vector2Int(1,2),
                orientation = Orientation.Horizontal,
            },

            //Vertical Win Lines
            new Line {
            gridVector2IntList = new List<Vector2Int>{ new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), },
                centerGridPosition = new Vector2Int(0,1),
                orientation = Orientation.Vertical,
            },
            new Line {
            gridVector2IntList = new List<Vector2Int>{ new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(1,2), },
                centerGridPosition = new Vector2Int(1,1),
                orientation = Orientation.Vertical,
            },
            new Line {
            gridVector2IntList = new List<Vector2Int>{ new Vector2Int(2,0), new Vector2Int(2,1), new Vector2Int(2,2), },
                centerGridPosition = new Vector2Int(2,1),
                orientation = Orientation.Vertical,
            },

            //Diagonal Win Lines
            new Line {
            gridVector2IntList = new List<Vector2Int>{ new Vector2Int(0,0), new Vector2Int(1,1), new Vector2Int(2,2), },
                centerGridPosition = new Vector2Int(1,1),
                orientation = Orientation.DiagonalA,
            },
            new Line {
            gridVector2IntList = new List<Vector2Int>{ new Vector2Int(0,2), new Vector2Int(1,1), new Vector2Int(2,0), },
                centerGridPosition = new Vector2Int(1,1),
                orientation = Orientation.DiagonalB,
            },
        };
    }

    //Runs on both client and server
    public override void OnNetworkSpawn(){
        Debug.Log("OnNetworkSpawn: " + NetworkManager.Singleton.LocalClientId);
        if(NetworkManager.Singleton.LocalClientId == 0) {
            localPlayerType = PlayerType.Cross;
        }
        else {
            localPlayerType = PlayerType.Circle;
        }
        //Runs only server
        if(IsServer) {
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        }
        //Server and Client
        currentPlayablePlayerType.OnValueChanged += (PlayerType oldPlayerType, PlayerType newPlayerType) => {
            OnCurrentPlayablePlayerTypeChanged?.Invoke(this, EventArgs.Empty);
        };
    }
    //Runs server
    //Server does validation to run the game
    private void NetworkManager_OnClientConnectedCallback(ulong obj){
        if (NetworkManager.Singleton.ConnectedClientsList.Count == 2){
            //Start Game
            currentPlayablePlayerType.Value = PlayerType.Cross;
            TriggerOnGameStartedRpc();
        }
    }
    //Runs both client and host
    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameStartedRpc() {
        OnGameStarted?.Invoke(this, EventArgs.Empty);
    }


    //Runs server/client
    [Rpc(SendTo.Server)]
    public void ClickedOnGridPositionRpc(int x, int y, PlayerType playerType) {
        Debug.Log("ClickedOnGridPosition " + x + ", " + y);
        if(playerType != currentPlayablePlayerType.Value) {
            return;
        }

        if(playerTypeArray[x,y] != PlayerType.None) {
            //Position is occupied
            return;
        }

        //Should now not play the same turn on the same grid position
        playerTypeArray[x, y] = playerType;
        TriggerOnPlacedObjectRpc();


        OnClickedOnGridPosition?.Invoke(this, new OnClickedOnGridPositionEventArgs {
            x = x,
            y = y,
            playerType = playerType,
        });
        
        switch (currentPlayablePlayerType.Value){
            default:
            case PlayerType.Cross:
                currentPlayablePlayerType.Value = PlayerType.Circle;
                break;
            case PlayerType.Circle:
                currentPlayablePlayerType.Value = PlayerType.Cross;
                break;


        }
        TestWinner();
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnPlacedObjectRpc() {
        OnPlacedObject?.Invoke(this, EventArgs.Empty);
    }

    private bool TestWinnerLine(Line line){
        return
        TestWinnerLine(
            playerTypeArray[line.gridVector2IntList[0].x, line.gridVector2IntList[0].y],
            playerTypeArray[line.gridVector2IntList[1].x, line.gridVector2IntList[1].y],
            playerTypeArray[line.gridVector2IntList[2].x, line.gridVector2IntList[2].y]
        );
    }

    private bool TestWinnerLine(PlayerType aPlayerType, PlayerType bPlayerType, PlayerType cPlayerType){
        return
            //Winner if 3 positions are exactly the same!
            aPlayerType != PlayerType.None &&
            aPlayerType == bPlayerType &&
            bPlayerType == cPlayerType;
    }
    private void TestWinner() {
        for(int i = 0; i < lineList.Count; i++) {
            Line line = lineList[i];
            if(TestWinnerLine(line)){
                Debug.Log("Winner!");
            currentPlayablePlayerType.Value = PlayerType.None;
            TriggerOnGameRpc(i, playerTypeArray[line.centerGridPosition.x, line.centerGridPosition.y]);
            break;
            }
        }

        //Cycle through gridmap if position is occupied
        //If none then we have a tie
        bool hasTie = true;
        for(int x = 0; x < playerTypeArray.GetLength(0); x++){
            for(int y = 0; y < playerTypeArray.GetLength(1); y++){
                if(playerTypeArray[x, y] == PlayerType.None) {
                    hasTie = false;
                    break;
                }
            }
        }
        if(hasTie) {
            TriggerOnGameTiedRpc();
            return;
        }
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameTiedRpc() {
        OnGameTied?.Invoke(this, EventArgs.Empty);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameRpc(int lineIndex, PlayerType winPlayerType) {
        Line line = lineList[lineIndex];
        OnGameWin?.Invoke(this, new OnGameWinEventArgs {
                line = line,
                winPlayerType = winPlayerType,
            });
    }
    //Resets the board only on the server
    [Rpc(SendTo.Server)]
    public void RematchRpc(){
        for(int x = 0; x < playerTypeArray.GetLength(0); x++) {
            for(int y = 0; y < playerTypeArray.GetLength(1); y++) {
                playerTypeArray[x, y] = PlayerType.None;
            }
        }
        currentPlayablePlayerType.Value = PlayerType.Cross;

        TriggerOnRematchRpc();
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnRematchRpc() {
        OnRematch?.Invoke(this, EventArgs.Empty);
    }
    public PlayerType GetLocalPlayerType() {
        return localPlayerType;
    }

    public PlayerType GetcurrentPlayablePlayerType(){
        return currentPlayablePlayerType.Value;
    }
}
