using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Group : MonoBehaviour
{
    float last_fall_time_;
    bool moving_left_ = false;
    bool moving_right_ = false;
    bool moving_down_ = false;
    float das_left_timer_ = 0;
    float das_right_timer_ = 0;
    float gravity_timer_ = 0;
    int gravity_score_ = 0;

    // 6 Hz delayed auto shift.
    const float kDAS = 1.0f / 3;
    // 30 Hz gravity.
    const float kGravity = 1.0f / 30;

    bool IsValidGridPosition()
    {
        foreach (Transform child in transform)
        {
            Vector2 v = Grid.RoundVec2(child.position);
            if (!Grid.InsideBorder(v)) return false;
            // Not part of any other grid block. Check for parent instead of
            // the block itself due to allowing rotations.
            if (Grid.grid[(int)v.x, (int)v.y] != null &&
                Grid.grid[(int)v.x, (int)v.y].parent != transform)
                return false;
        }
        return true;
    }

    void UpdateGrid()
    {
        // Delete old blocks of group from position in grid.
        for (int y = 0; y < Grid.h; y++)
        {
            for (int x = 0; x < Grid.w; x++)
            {
                if (Grid.grid[x, y] != null &&
                    Grid.grid[x, y].parent == transform)
                    Grid.grid[x, y] = null;
            }
        }

        // Add new blocks of group to position in grid.
        foreach (Transform child in transform)
        {
            Vector2 v = Grid.RoundVec2(child.position);
            Grid.grid[(int)v.x, (int)v.y] = child;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        // Invalid spawn position means game over.
        if (!IsValidGridPosition())
        {
            Debug.Log("GAME OVER");
            Grid.score = 0;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    // Move and return whether move was valid.
    bool Move(Vector3 v)
    {
        transform.position += v;
        if (IsValidGridPosition())
        {
            UpdateGrid();
            return true;
        }
        transform.position -= v;
        return false;
    }

    void MoveDown()
    {
        if (!Move(new Vector3(0, -1, 0)))
        {
            // Clear grid.
            Grid.DeleteFullRows(gravity_score_);
            gravity_score_ = 0;
            // Start next group.
            FindObjectOfType<Spawner>().SpawnNext();
            // It's over for us, buddy :).
            enabled = false;
        }
        last_fall_time_ = Time.time;
    }

    void Rotate()
    {
        transform.Rotate(0, 0, -90);
        if (IsValidGridPosition())
            UpdateGrid();
        else
            transform.Rotate(0, 0, 90);
    }

    void StartLeft()
    {
        Move(new Vector3(-1, 0, 0));
        moving_left_ = true;
        das_left_timer_ = 0;
    }

    void StartRight()
    {
        Move(new Vector3(1, 0, 0));
        moving_right_ = true;
        das_right_timer_ = 0;
    }

    void StartDown()
    {
        MoveDown();
        moving_down_ = true;
        gravity_timer_ = 0;
        gravity_score_++;
    }

    void KeyboardUpdate()
    {
        // Up / Rotate
        if (Input.GetKeyDown(KeyCode.UpArrow)) Rotate();
        // Left
        if (Input.GetKeyDown(KeyCode.LeftArrow)) StartLeft();
        else if (Input.GetKeyUp(KeyCode.LeftArrow)) moving_left_ = false;
        // Right
        if (Input.GetKeyDown(KeyCode.RightArrow)) StartRight();
        else if (Input.GetKeyUp(KeyCode.RightArrow)) moving_right_ = false;
        // Down
        if (Input.GetKeyDown(KeyCode.DownArrow)) StartDown();
        else if (Input.GetKeyUp(KeyCode.DownArrow)) moving_down_ = false;
    }

    // Given pixel coordinates of a touch, return which equivalent key was
    // pressed. Diagram:
    // \UU/
    // L\/R
    // L/\R
    // /DD\
    KeyCode TouchToEquivalentKey(Vector2 v)
    {
        float slope = (float)(Screen.height) / Screen.width;
        bool up_right = Screen.height - slope * v.x < v.y;
        if (slope * v.x < v.y)  // up left half of screen
        {
            if (up_right) return KeyCode.UpArrow;
            else return KeyCode.LeftArrow;
        } else
        {
            if (up_right) return KeyCode.RightArrow;
            else return KeyCode.DownArrow;
        }
    }

    void TouchUpdate()
    {

        foreach (var touch in Input.touches)
        {
            KeyCode equivalent_key = TouchToEquivalentKey(touch.position);
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    switch (equivalent_key)
                    {
                        case KeyCode.UpArrow:
                            Rotate();
                            break;
                        case KeyCode.LeftArrow:
                            StartLeft();
                            break;
                        case KeyCode.RightArrow:
                            StartRight();
                            break;
                        case KeyCode.DownArrow:
                            StartDown();
                            break;
                    }
                    break;
                case TouchPhase.Ended:
                    switch (equivalent_key)
                    {
                        case KeyCode.LeftArrow:
                            moving_left_ = false;
                            break;
                        case KeyCode.RightArrow:
                            moving_right_ = false;
                            break;
                        case KeyCode.DownArrow:
                            moving_down_ = false;
                            break;
                    }
                    break;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        KeyboardUpdate();
        TouchUpdate();
        // Push block down.
        if (Time.time - last_fall_time_ >= 1)
            MoveDown();
    }

    // Move only if timer has passed kDAS, kDAS + kGravity,
    // kDAS + 2 * kGravity, ...
    void MoveDAS(float timer, Vector3 v)
    {
        float since_das = timer - kDAS;
        float after_since_das = since_das + Time.deltaTime;
        if (after_since_das >= 0 &&
            (int)(after_since_das / kGravity) >=
            (int)(since_das / kGravity))
        {
            Move(v);
        }
    }

    void FixedUpdate()
    {
        if (moving_left_)
        {
            MoveDAS(das_left_timer_, new Vector3(-1, 0, 0));
            das_left_timer_ += Time.deltaTime;
        }
        if (moving_right_)
        {
            MoveDAS(das_right_timer_, new Vector3(1, 0, 0));
            das_right_timer_ += Time.deltaTime;
        }
        if (moving_down_)
        {
            gravity_timer_ += Time.deltaTime;
            if (gravity_timer_ > kGravity)
            {
                MoveDown();
                gravity_timer_ = 0;
                gravity_score_++;
            }
        }
    }
}