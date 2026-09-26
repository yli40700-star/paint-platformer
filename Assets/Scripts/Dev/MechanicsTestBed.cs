using UnityEngine;

/// <summary>
/// A's private test course for the player + color mechanics (not the real level; B builds that).
/// How to use: duplicate SampleScene (Cmd+D) as "A_TestScene", open it, add an empty GameObject,
/// attach this component and press Play. Don't commit changes to SampleScene.
///
/// Map legend: . air  # ground  B/G/R ground pre-painted blue/green/red  ^ floor spikes  v ceiling spikes  P player
/// What it tests: blue long jump over the pit, green bounce into ceiling spikes (trap) vs. green
/// in front of the wall (correct), and red killing you on the ledge.
/// </summary>
public class MechanicsTestBed : MonoBehaviour
{
    public string[] map =
    {
        "........................",
        "..............##########",
        ".....vvvvv..............",
        "........................",
        ".............#####R#####",
        "P............###########",
        "####BB^^^^##G###########",
        "########################",
    };

    Sprite square;

    void Awake()
    {
        square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
        var root = new GameObject("TestCourse").transform;

        for (int r = 0; r < map.Length; r++)
            for (int c = 0; c < map[r].Length; c++)
            {
                char ch = map[r][c];
                Vector3 center = new Vector3(c + 0.5f, -r - 0.5f, 0f);
                switch (ch)
                {
                    case '#': MakeTile(root, center, PaintColor.None); break;
                    case 'B': MakeTile(root, center, PaintColor.Blue); break;
                    case 'G': MakeTile(root, center, PaintColor.Green); break;
                    case 'R': MakeTile(root, center, PaintColor.Red); break;
                    case '^': MakeSpike(root, center, true); break;
                    case 'v': MakeSpike(root, center, false); break;
                    case 'P': MakePlayer(new Vector3(c + 0.5f, -r - 1f + 0.46f, 0f)); break;
                }
            }

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.orthographic = true;
            float w = map[0].Length, h = map.Length;
            cam.orthographicSize = Mathf.Max(h / 2f + 1f, (w / 2f + 1f) / cam.aspect);
            cam.transform.position = new Vector3(w / 2f, -h / 2f, -10f);
        }
    }

    SpriteRenderer MakeBlock(Transform parent, string name, Vector3 pos, Vector2 scale, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = color;
        return sr;
    }

    static Color ColorOf(PaintColor p)
    {
        switch (p)
        {
            case PaintColor.Blue: return new Color(0.23f, 0.51f, 0.96f);
            case PaintColor.Green: return new Color(0.13f, 0.77f, 0.37f);
            case PaintColor.Red: return new Color(0.94f, 0.27f, 0.27f);
            default: return new Color(0.4f, 0.45f, 0.53f);
        }
    }

    void MakeTile(Transform root, Vector3 pos, PaintColor paint)
    {
        SpriteRenderer sr = MakeBlock(root, "Tile", pos, Vector2.one, ColorOf(paint));
        sr.gameObject.AddComponent<BoxCollider2D>();
        Tile t = sr.gameObject.AddComponent<Tile>();
        t.Paint = paint;
        t.PaintChanged += tile => sr.color = ColorOf(tile.Paint);
    }

    void MakeSpike(Transform root, Vector3 cellCenter, bool floor)
    {
        // Visual + trigger covering the pointy 55% of the cell.
        float h = 0.55f;
        float y = floor ? cellCenter.y - 0.5f + h / 2f : cellCenter.y + 0.5f - h / 2f;
        SpriteRenderer sr = MakeBlock(root, floor ? "Spike" : "CeilingSpike", new Vector3(cellCenter.x, y, 0f),
            new Vector2(0.7f, h), new Color(0.8f, 0.84f, 0.88f));
        sr.gameObject.AddComponent<BoxCollider2D>().isTrigger = true;
        sr.gameObject.AddComponent<Hazard>().reason = floor ? "Spiked!" : "Hit the ceiling spikes!";
    }

    void MakePlayer(Vector3 pos)
    {
        var go = new GameObject("Player");
        go.transform.position = pos;
        go.transform.localScale = new Vector3(0.7f, 0.9f, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = square;
        sr.color = Color.white;
        sr.sortingOrder = 10;
        go.AddComponent<BoxCollider2D>();
        go.AddComponent<Rigidbody2D>();
        go.AddComponent<PlayerController>();
        go.AddComponent<ColorEffects>();
        PlayerController.Died += reason => Debug.Log("[TestBed] Player died: " + reason);
    }
}
