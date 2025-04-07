using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PictureManager : MonoBehaviour
{
    public Picture PictureClone; //Reference to the Picture prefab to be instantiated
    public Transform PicSquawnPosition; //Position where the pictures will initially be spawned
    public Vector2 StartPosition = new Vector2(-2.5f,-3.62f); // Starting position for the first picture

    public enum GameState
    {
        NoAction,
        MovingOnPositions,
        DeletePuzzles,
        FlipBack,
        Checking,
        GameEnd
    };

    public enum PuzzleState
    {
        PuzzleRotating,
        CanRotate
    };

    public enum RevealedState
    {
        NoRevealed,
        OneRevealed,
        TwoRevealed
    };
    [HideInInspector]
    public GameState CurrentGameState;
    [HideInInspector]
    public PuzzleState CurrentPuzzleState;
    [HideInInspector]
    public RevealedState PuzzledRevealedNumber;
    //List to hold all the instantiated pictures
    [HideInInspector]
    public List<Picture> PictureList;

    private Vector2 _offset = new Vector2(2.5f, 2.52f);
    private Vector2 _offsetFor15Pairs = new Vector2(2.5f, 2.5f);
    private Vector2 _offsetFor20Pairs = new Vector2(2f, 2f);
    private Vector3 _newScaleDown = new Vector3(1.5f, 1.5f, 1.5f);

    private List<Material> _materialList = new List<Material>();
    private List<string> _texturePathList = new List<string>();
    private Material _firstMaterial;
    private string _firstTexturePath;

    private int _firstRevealedPic;
    private int _secondRevealedPic;
    private int _revealedPicNumber = 0;
    private int _picToDestroy1;
    private int _picToDestroy2;

    private bool _corutineStarted = false;

    void Start()
    {
        CurrentGameState = GameState.NoAction;
        CurrentPuzzleState = PuzzleState.CanRotate;
        PuzzledRevealedNumber = RevealedState.NoRevealed;

        _revealedPicNumber = 0;
        _firstRevealedPic = -1;
        _secondRevealedPic = -1;


        LoadMaterials();
        if (GameSettings.Instance.GetPairNumber() == GameSettings.EPairNumber.E10Pairs)
        {
            CurrentGameState = GameState.MovingOnPositions;
            SpawnPictureMesh(rows: 4, colums: 5, Pos: StartPosition, _offset, scaleDown: false);
            MovePicture(rows: 4, colums: 5, pos: StartPosition, _offset);
        }
        else if (GameSettings.Instance.GetPairNumber() == GameSettings.EPairNumber.E15Pairs)
        {
            CurrentGameState = GameState.MovingOnPositions;
            SpawnPictureMesh(rows: 5, colums: 6, Pos: StartPosition, _offset, scaleDown: false);
            MovePicture(rows: 5, colums: 6, pos: StartPosition, _offsetFor15Pairs);
        }
        else if (GameSettings.Instance.GetPairNumber() == GameSettings.EPairNumber.E20Pairs)
        {
            CurrentGameState = GameState.MovingOnPositions;
            SpawnPictureMesh(rows: 5, colums: 8, Pos: StartPosition, _offset, scaleDown: true);
            MovePicture(rows: 5, colums: 8, pos: StartPosition, _offsetFor20Pairs);
        }
    }

    public void CheckPicture()
    {
        CurrentGameState = GameState.Checking;
        _revealedPicNumber = 0;
        for(int id = 0; id<PictureList.Count; id++)
        {
            if(PictureList[id].Revealed && _revealedPicNumber<2)
            {
                if(_revealedPicNumber == 0)
                {
                    _firstRevealedPic = id;
                    _revealedPicNumber++;
                }
                else if (_revealedPicNumber == 1)
                {
                    _secondRevealedPic = id;
                    _revealedPicNumber++;
                }
            }
        }
        if (_revealedPicNumber == 2)
        {
            if (PictureList[_firstRevealedPic].GetIndex() == PictureList[_secondRevealedPic].GetIndex() && _firstRevealedPic != _secondRevealedPic)
            {
                PictureList[_firstRevealedPic].PlayMatchSound(); //  Play match sound
                CurrentGameState = GameState.DeletePuzzles;
                _picToDestroy1 = _firstRevealedPic;
                _picToDestroy2 = _secondRevealedPic;
            }
            else
            {
                PictureList[_firstRevealedPic].PlayMismatchSound(); //  Play mismatch sound
                CurrentGameState = GameState.FlipBack;
            }
        }
        CurrentPuzzleState = PictureManager.PuzzleState.CanRotate;

        if(CurrentGameState == GameState.Checking)
        {
            CurrentGameState = GameState.NoAction;
        }
    }
    private void DestroyPicture()
    {
        PuzzledRevealedNumber = RevealedState.NoRevealed;
        
        PictureList[_picToDestroy1].Deactivate();
        PictureList[_picToDestroy2].Deactivate();
        _revealedPicNumber = 0;
        CurrentGameState = GameState.NoAction;
        CurrentPuzzleState = PuzzleState.CanRotate;
    }
    private IEnumerator FlipBack()
    {
        _corutineStarted = true;

        yield return new WaitForSeconds(0.5f);

        PictureList[_firstRevealedPic].Flipback();
        PictureList[_secondRevealedPic].Flipback();

        PictureList[_firstRevealedPic].Revealed = false;
        PictureList[_secondRevealedPic].Revealed = false;

        PuzzledRevealedNumber = RevealedState.NoRevealed;
        CurrentGameState = GameState.NoAction;

        _corutineStarted = false;
    }
   private void LoadMaterials()
   {
        var materialFilePath = GameSettings.Instance.GetMaterialDirectoryName();
        var textureFilePath = GameSettings.Instance.GetPuzzleCategoryTextureDirectoryName();
        var pairNumber = (int)GameSettings.Instance.GetPairNumber();
        const string matBaseName = "Pic";
        var firstMaterialName = "Back";

        for (var index = 1; index <= pairNumber; index++)
        {
            var currentFilePath = materialFilePath + matBaseName + index;
            Material mat = Resources.Load(currentFilePath, typeof(Material)) as Material;
            _materialList.Add(mat);

            var currentTextureFilePath = textureFilePath + matBaseName + index;
            _texturePathList.Add(currentTextureFilePath);
        }
        _firstTexturePath = textureFilePath + firstMaterialName;
        _firstMaterial = Resources.Load(materialFilePath + firstMaterialName, typeof(Material)) as Material;
    }

    void Update()
    {
        if(CurrentGameState == GameState.DeletePuzzles)
        {
            if(CurrentPuzzleState == PuzzleState.CanRotate)
            {
                DestroyPicture();
            }
        }

        if(CurrentGameState == GameState.FlipBack)
        {
            if (CurrentPuzzleState == PuzzleState.CanRotate && _corutineStarted == false)
            {
                StartCoroutine(FlipBack());
            }
        }
    }


    private void SpawnPictureMesh(int rows, int colums, Vector2 Pos, Vector2 offset, bool scaleDown)
    {
        for (int col = 0; col < colums; col++)
        {
            for (int row = 0; row < rows; row++)
            {
                var tempPicture = (Picture)Instantiate(PictureClone, PicSquawnPosition.position, PictureClone.transform.rotation);

                if (scaleDown)
                {
                    tempPicture.transform.localScale = _newScaleDown;
                }

                tempPicture.name = tempPicture.name + 'c' + col + 'r' + row;
                PictureList.Add(tempPicture);
            }
        }
        ApplyTextures();
    }
    

    public void ApplyTextures()
     {
         var rndMatIndex = Random.Range(0, _materialList.Count);
         var AppliedTimes = new int[_materialList.Count];

         for (int i = 0; i < _materialList.Count; i++)
         {
             AppliedTimes[i] = 0;
         }
         foreach (var o in PictureList)
         {
             var randPrevious = rndMatIndex;
             var counter = 0;
             var forceMat = false;

             while (AppliedTimes[rndMatIndex] >= 2 || ((randPrevious == rndMatIndex) && !forceMat))
             {
                 rndMatIndex = Random.Range(0, _materialList.Count);
                 counter++;
                 if (counter > 100)
                 {
                     for (var j = 0; j < _materialList.Count; j++)
                     {
                         if (AppliedTimes[j] < 2)
                         {
                             rndMatIndex = j;
                             forceMat = true;
                         }
                     }
                     if (forceMat == false)
                         return;
                 }
             }
             o.SetFirstMaterial(_firstMaterial, _firstTexturePath);
             o.ApplyFirstMaterial();
             o.SetSecondMaterial(_materialList[rndMatIndex], _texturePathList[rndMatIndex]);
            o.SetIndex(rndMatIndex);
            o.Revealed = false;
            

             AppliedTimes[rndMatIndex] += 1;
             forceMat = false;
         }
     }
    private void MovePicture(int rows, int colums, Vector2 pos, Vector2 offset)
    {
        var index = 0;
        for(var col = 0; col < colums; col++)
        {
            for(int row = 0; row < rows; row++)
            {
                var targetPosition = new Vector3((pos.x + (offset.x * row)), (pos.y + (offset.y * col)), 0.0f);
                StartCoroutine(MoveToPosition(targetPosition, PictureList[index]));
                index++;
            }

        }
    }
    private IEnumerator MoveToPosition(Vector3 target, Picture obj)
    {
        var randomDis = 7;
        while(obj.transform.position != target)
        {
            obj.transform.position = Vector3.MoveTowards(obj.transform.position, target, randomDis * Time.deltaTime);
            yield return 0;
        }
    }
}
