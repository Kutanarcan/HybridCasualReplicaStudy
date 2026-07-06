# How the Ball Sort Solver Works (BFS, visualized)

This explains what [`BallSortSolver.Solve`](Runtime/BallSortSolver.cs) actually does — not the
bit-packing tricks, but the **idea**: how a puzzle becomes a graph, and how Breadth‑First Search
walks that graph to find the *shortest* solution.

---

## 1. The key mental shift: a puzzle is a graph

Forget "algorithm" for a second. Picture the puzzle as a huge map of **rooms connected by doors**.

- A **room = one complete board position** (every bar, every ball, exactly where they are right now).
- A **door = one legal move** (pour the top ball of some bar onto a legal destination bar).
- Walking through a door takes you to a *new* room (a new board position).
- The **exit** is any room where the board is solved (every bar full and single‑colored).

So "solve the puzzle" becomes: **find a path of doors from the starting room to an exit.**
And "solve it in the fewest moves" becomes: **find the *shortest* path.**

That reframing is the whole trick. Once it's a shortest-path problem on a graph, BFS is the standard tool.

---

## 2. Why BFS specifically?

BFS explores the map **in rings**, by distance from the start:

- **Ring 0** = the start room.
- **Ring 1** = every room reachable in **1 move**.
- **Ring 2** = every room reachable in **2 moves**.
- …and so on.

It finishes an entire ring before touching the next one. So the **first time** BFS steps into an
exit room, it's guaranteed that no shorter path exists — you reached it on the earliest possible ring.

> That "first time we see the goal = shortest" guarantee is *the* reason we use BFS instead of just
> randomly trying moves. The ring number where we find the exit **is** the minimum move count
> (`SolveResult.MinMoves`).

In the code, a "ring" is called `frontier`, and the ring number is `depth`:

```csharp
var frontier = new List<ulong[]> { initial };   // ring 0 = just the start
int depth = 0;
while (frontier.Count > 0)
{
    depth++;                          // move to the next ring
    var next = new List<ulong[]>();   // the ring we're about to build
    foreach (var state in frontier)   // for every room in the current ring...
        // ...open every door, collect the rooms on the far side into `next`
    frontier = next;                  // advance one ring outward
}
```

---

## 3. A tiny worked example (trace it by hand)

Let's use the smallest interesting puzzle so the whole graph fits on screen.

- **2 colors:** `R` and `G`
- **Bar height:** 2 (a bar is done when it holds 2 of the same color)
- **3 bars:** two mixed, one empty

Notation: a bar is written **bottom → top**, so `RG` means `R` on the bottom, `G` on top.
`__` is an empty bar. You may only pour the **top** ball.

**Start:** `RG | GR | __`

Here is the *entire* search BFS performs. Each arrow is one legal move (one "door"):

```
Ring 0 (start):           RG | GR | __
                         /              \
                (pour G from bar1)   (pour R from bar2)
                       /                  \
Ring 1:        R | GR | G            RG | G | R
                    |                     |
             (pour R from bar2      (pour G from bar1
              onto bar1)             onto bar2)
                    |                     |
Ring 2:      RR | G | G            R | GG | R
                    |                     |
             (pour G from bar2     (pour R from bar1
              onto bar3)            onto bar3)
                    |                     |
Ring 3:   RR | __ | GG  ✅        __ | GG | RR  ✅
          (SOLVED, 3 moves)      (also solved, 3 moves)
```

BFS reaches a solved room for the **first time on Ring 3**, so the answer is **minimum = 3 moves**.
(Both branches happen to solve in 3; BFS simply returns the first one it lands on.)

### The exact counts BFS produced

These are the real numbers logged from the actual solver on this puzzle:

| Ring (`depth`) | Rooms coming in (`frontier`) | New rooms found (`next`) | Total rooms seen (`visited`) | Doors opened (move attempts) |
|:---:|:---:|:---:|:---:|:---:|
| 0 | — | 1 (start) | 1 | — |
| 1 | 1 | 2 | 3 | 2 |
| 2 | 2 | 2 | 5 | 2 |
| 3 | 2 | goal hit | 6 | 1 |

**Result:** `MinMoves = 3`, `StatesExplored = 6`, total move attempts = `5`.

Notice how *narrow* the tree is — only 6 rooms total. That's not luck; it's the pruning in Section 4.

---

## 4. What keeps the tree from exploding

A naive version would open a lot of pointless doors. The solver skips them. Three rules, all visible
in `Solve`:

1. **Don't pour from a finished bar.**
   `if (IsComplete(state[from], height)) continue;`
   A bar that's already full and single‑colored is *done* — touching it can only make things worse.

2. **Only one empty bar is worth trying as a destination.**
   `if (usedEmpty) continue; usedEmpty = true;`
   If there are three empty bars, pouring into empty‑bar‑A vs empty‑bar‑B vs empty‑bar‑C all lead to
   the *same* board (empty bars are interchangeable). So we try exactly one and ignore the clones.

3. **Never pour a same-colored stack into an empty bar.**
   `if (IsUniform(state[from])) continue;` (only checked when the destination is empty)
   Moving `GG` from one bar into an empty bar just shuffles it sideways — same position, wasted move.

Together these cut the branching factor dramatically, which is why an "easy" puzzle explores dozens
of rooms instead of millions.

---

## 5. The other half of the trick: never visit the same room twice

Two different move orders often land on the **identical board**. Without a memory, BFS would re‑expand
the same room over and over and loop forever. The `visited` set prevents that:

```csharp
var key = new StateKey(child);
if (visited.Contains(key)) continue;   // seen this exact board before → ignore
visited.Add(key);
```

One subtlety: **the order of the bars doesn't matter.** `RG | GR | __` is the same puzzle as
`__ | GR | RG` — you'd just be looking at the same three bars from a different angle. To make those
count as one room, `StateKey` **sorts the bars** before comparing/hashing:

```csharp
_bars = (ulong[])bars.Clone();
Array.Sort(_bars);          // canonical order → reorderings collapse into one key
```

Without this, the search space would balloon by a factor of (number of bars)! for no reason.

---

## 6. So how many iterations for an "easy" puzzle?

Here's the actual predefined starter level (3 colors, height 4, 1 empty), the same one from the
runtime example:

```
Start:  RRYG | YYGR | GGRY | __        (each bar bottom → top)
Goal:   RRRR | YYYY | GGGG | __        (each color gathered)
```

Real solver output:

| Metric | Value |
|---|---|
| Minimum moves (`MinMoves` = final ring) | **10** |
| Rooms actually explored (`StatesExplored`) | **30** |
| Total doors opened (move attempts) | ~43 |
| Safety cap (`MaxBfsStates`) | 600,000 |

So an easy level is solved after touching only **~30 board positions across 10 rings** — a rounding
error compared to the 600,000-state ceiling. Hard, highly‑fragmented levels are what push toward that
cap; when they blow past it, `Solve` bails out with `SolveStatus.Timeout` instead of hanging.

**Rule of thumb:** number of BFS rings = the puzzle's optimal move count. States explored grows with
difficulty (how tangled the board is), not just with move count.

---

## 7. Reading it back in the code

| Concept here | In [`BallSortSolver.cs`](Runtime/BallSortSolver.cs) |
|---|---|
| A room (board position) | a `ulong[]` — one packed number per bar |
| The current ring | `frontier` |
| The ring being built | `next` |
| Ring number = moves so far | `depth` |
| "Have I seen this room?" | `visited` (a `HashSet<StateKey>`) |
| Room identity ignoring bar order | `StateKey` (sorts the bars) |
| Opening a door (making a move) | clone the state, `Push` onto dest, `Pop` from source |
| Reached an exit | `IsSolved(child, height)` → return `Solvable`, `MinMoves = depth` |
| Ran out of rooms | loop ends → `Unsolvable` |
| Too many rooms | `visited.Count >= MaxBfsStates` → `Timeout` |

---

## 8. Appendix: why each bar is a `ulong` (the packing)

This part is pure performance and is *orthogonal* to the BFS idea above — BFS would work identically
if a bar were a `List<int>`. But BFS clones and hashes board positions **constantly**, so the solver
stores each bar as a single integer instead:

- Balls are packed **4 bits each** (a color 0–9 fits in a nibble), bottom → top.
- Each stored value is `color + 1`, so a real color is never `0`.
- A leading **sentinel nibble** (`1`) sits above the top ball. Because of it, the ball count is
  recoverable from the number's length, so `Top`, `Count`, `Push`, `Pop` are all one or two bitwise ops.

Example — the bar `R G` (with `R=0`, `G=1`), read bottom → top:

```
start:  0x1                 (just the sentinel)
push R: 0x1 << 4 | (0+1) =  0x11
push G: 0x11 << 4 | (1+1) = 0x112     ← the whole bar is now one number: 0x112
```

`Pop` is `>> 4`, `Top` is `& 0xF` minus 1, `Push` is `<< 4 | (color+1)`. Cloning a whole board is
then just copying a small `ulong[]` — which is what makes exploring tens of thousands of rooms cheap.
