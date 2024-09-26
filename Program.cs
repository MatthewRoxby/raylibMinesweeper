using Raylib_cs;
using System.Numerics;

const int BOMB_MAX_PERCENT = 20;
const int BOMB_MIN_PERCENT = 12;

int canvasWidth = 800, canvasHeight = 800;

int gridWidth = 15, gridHeight = 15;

int cellSize;

float canvasOffsetX, canvasOffsetY;

int mouseCellPosX = 0, mouseCellPosY = 0;

Color covered1 = new Color(111, 29, 92, 255);
Color covered2 = new Color(110, 81, 129, 255);
Color uncovered1 = new Color(109, 133, 165, 255);
Color uncovered2 = new Color(108, 185, 201, 255);

CELL_STATE[,] board;

int[,] boardNumbers;

Raylib.InitWindow(820, 920, "Minesweeper");

Raylib.SetTargetFPS(60);

Texture2D topPanelTex = Raylib.LoadTexture("sprites/topPanel.png");
Texture2D bottomPanelTex = Raylib.LoadTexture("sprites/bottomPanel.png");
Texture2D numbersTex = Raylib.LoadTexture("sprites/numbers.png");
Texture2D flagTex = Raylib.LoadTexture("sprites/flag.png");
Texture2D bombTex = Raylib.LoadTexture("sprites/bomb.png");

Font font = Raylib.LoadFont("font.ttf");

GAME_STATE gameState = GAME_STATE.PLAYING;

bool firstClick = false;

List<ParticleBox> boxParticles = new List<ParticleBox>();

void AddBoxParticle(Vector2 position, Color c, bool isFlag = false)
{
	ParticleBox pb = new ParticleBox();
	pb.isFlag = isFlag;

	pb.position = position;
	pb.col = c;

	pb.velocity = new Vector2(Raylib.GetRandomValue(-100, 100), Raylib.GetRandomValue(-500, -100));
	pb.angularVel = Raylib.GetRandomValue(-180, 180);
	boxParticles.Add(pb);
}

void DrawBoxParticles()
{
	float delta = Raylib.GetFrameTime();
	List<ParticleBox> toDelete = new List<ParticleBox>();
	foreach (ParticleBox pb in boxParticles)
	{
		pb.position += pb.velocity * delta;
		pb.rot += pb.angularVel * delta;
		pb.velocity.Y += 980f * delta;

		if (pb.position.Y > 1100) toDelete.Add(pb);
		else
		{
			if (pb.isFlag)
			{
				Raylib.DrawTexturePro(flagTex, new Rectangle(0, 0, 8, 8), new Rectangle(pb.position.X, pb.position.Y, cellSize, cellSize), Vector2.One * cellSize * 0.5f, pb.rot, pb.col);
			}
			else
			{
				Raylib.DrawRectanglePro(new Rectangle(pb.position.X, pb.position.Y, cellSize, cellSize), Vector2.One * (float)cellSize * 0.5f, pb.rot, pb.col);
			}
		}

	}

	foreach (ParticleBox p in toDelete)
	{
		boxParticles.Remove(p);
	}
}

void SetCellSize()
{
	cellSize = (int)MathF.Min(canvasWidth / (float)gridWidth, canvasHeight / (float)gridHeight);
	canvasOffsetX = 10 + (canvasWidth - (gridWidth * cellSize)) * 0.5f;
	canvasOffsetY = 110 + (canvasHeight - (gridHeight * cellSize)) * 0.5f;
}

void ResetBoard()
{
	board = new CELL_STATE[gridWidth, gridHeight];
	boardNumbers = new int[gridWidth, gridHeight];
	float bombPercent = Raylib.GetRandomValue(BOMB_MIN_PERCENT, BOMB_MAX_PERCENT) / 100.0f;
	int bombsToSpawn = (int)(bombPercent * (float)(gridWidth * gridHeight));

	firstClick = true;
	while (bombsToSpawn > 0)
	{
		int x = Raylib.GetRandomValue(0, gridWidth - 1);
		int y = Raylib.GetRandomValue(0, gridHeight - 1);
		if (board[x, y] == CELL_STATE.COVERED_CELL)
		{
			board[x, y] = CELL_STATE.COVERED_BOMB;
			bombsToSpawn--;
		}
	}

}

void RevealAllBombs()
{
	for (int x = 0; x < gridWidth; x++)
	{
		for (int y = 0; y < gridHeight; y++)
		{
			if (board[x, y] == CELL_STATE.COVERED_BOMB)
			{
				board[x, y] = CELL_STATE.UNCOVERED_BOMB;
				bool colourToggle = ((x * gridHeight + y) % 2) == 0;
				AddBoxParticle(new Vector2(canvasOffsetX + ((float)x + 0.5f) * cellSize, canvasOffsetY + ((float)y + 0.5f) * cellSize), colourToggle ? covered1 : covered2);
			}
			else if (board[x, y] == CELL_STATE.FLAGGED_BOMB)
			{
				board[x, y] = CELL_STATE.UNCOVERED_BOMB;
				bool colourToggle = ((x * gridHeight + y) % 2) == 0;
				AddBoxParticle(new Vector2(canvasOffsetX + ((float)x + 0.5f) * cellSize, canvasOffsetY + ((float)y + 0.5f) * cellSize), colourToggle ? covered1 : covered2);
				AddBoxParticle(new Vector2(canvasOffsetX + ((float)x + 0.5f) * cellSize, canvasOffsetY + ((float)y + 0.5f) * cellSize), Color.White, true);
			}
		}
	}
}

SetCellSize();
ResetBoard();

void DrawBanner()
{
	Raylib.DrawTextureEx(topPanelTex, Vector2.Zero, 0.0f, 10.0f, Color.White);
	Raylib.DrawTextEx(font, "   new\n\ngame", new Vector2(20, 20), 32, 0, Color.White);
}


void DrawBackground()
{
	Raylib.DrawTextureEx(bottomPanelTex, new Vector2(0, 100), 0.0f, 10.0f, Color.White);
}

int GetNumbers(int x, int y)
{
	int result = 0;

	for (int xo = -1; xo <= 1; xo++)
	{
		for (int yo = -1; yo <= 1; yo++)
		{
			if (xo == 0 && yo == 0) continue;

			if (x + xo < 0 || x + xo >= gridWidth || y + yo < 0 || y + yo >= gridHeight) continue;

			if (board[x + xo, y + yo] == CELL_STATE.COVERED_BOMB || board[x + xo, y + yo] == CELL_STATE.FLAGGED_BOMB) result++;
		}
	}

	return result;
}

void DrawBoard()
{

	for (int x = 0; x < gridWidth; x++)
	{
		for (int y = 0; y < gridHeight; y++)
		{
			bool colourToggle = ((x * gridHeight + y) % 2) == 0;
			if (Raylib.IsKeyPressed(KeyboardKey.A)) Console.WriteLine(x * gridHeight + y);
			switch (board[x, y])
			{
				case CELL_STATE.COVERED_BOMB:
				case CELL_STATE.COVERED_CELL:
					Raylib.DrawRectangleV(new Vector2(canvasOffsetX + x * cellSize, canvasOffsetY + y * cellSize), Vector2.One * cellSize, colourToggle ? covered1 : covered2);
					break;
				case CELL_STATE.UNCOVERED_BOMB:
					Raylib.DrawRectangleV(new Vector2(canvasOffsetX + x * cellSize, canvasOffsetY + y * cellSize), Vector2.One * cellSize, colourToggle ? uncovered1 : uncovered2);
					Raylib.DrawTexturePro(bombTex, new Rectangle(0, 0, 8, 8), new Rectangle(canvasOffsetX + x * cellSize, canvasOffsetY + y * cellSize, cellSize, cellSize), Vector2.Zero, 0f, Color.White);
					break;
				case CELL_STATE.UNCOVERED_CELL:
					Raylib.DrawRectangleV(new Vector2(canvasOffsetX + x * cellSize, canvasOffsetY + y * cellSize), Vector2.One * cellSize, colourToggle ? uncovered1 : uncovered2);
					int number = boardNumbers[x, y];
					if (number > 0)
					{
						Raylib.DrawTexturePro(numbersTex, new Rectangle((number - 1) * 8, 0, 8, 8), new Rectangle(canvasOffsetX + x * cellSize, canvasOffsetY + y * cellSize, cellSize, cellSize), Vector2.Zero, 0f, Color.White);
					}
					break;
				case CELL_STATE.FLAGGED_BOMB:
				case CELL_STATE.FLAGGED_CELL:
					Raylib.DrawRectangleV(new Vector2(canvasOffsetX + x * cellSize, canvasOffsetY + y * cellSize), Vector2.One * cellSize, colourToggle ? covered1 : covered2);
					Raylib.DrawTexturePro(flagTex, new Rectangle(0, 0, 9, 9), new Rectangle(canvasOffsetX + x * cellSize, canvasOffsetY + y * cellSize, cellSize, cellSize), Vector2.Zero, 0f, Color.White);
					break;
				default:
					break;
			}
		}
	}
}

void FloodFillClear(int x, int y)
{
	Queue<Vector2> q = new Queue<Vector2>();
	q.Enqueue(new Vector2(x, y));

	Console.WriteLine(q.Count);
	while (q.Count != 0)
	{
		Console.WriteLine("erm");
		Vector2 n = q.Dequeue();
		int nx = (int)n.X;
		int ny = (int)n.Y;
		if (nx < 0 || nx >= gridWidth || ny < 0 || ny >= gridHeight) continue;
		if (board[nx, ny] == CELL_STATE.COVERED_CELL)
		{
			board[nx, ny] = CELL_STATE.UNCOVERED_CELL;
			bool colourToggle = ((nx * gridHeight + ny) % 2) == 0;
			AddBoxParticle(new Vector2(canvasOffsetX + (n.X + 0.5f) * cellSize, canvasOffsetY + (n.Y + 0.5f) * cellSize), colourToggle ? covered1 : covered2);

			int b = boardNumbers[nx, ny] = GetNumbers(nx, ny);

			if (b > 0) continue;
			q.Enqueue(n + new Vector2(1, 0));
			q.Enqueue(n + new Vector2(0, 1));
			q.Enqueue(n + new Vector2(-1, 0));
			q.Enqueue(n + new Vector2(0, -1));
		}
	}
}

void CheckWin()
{
	bool hasWon = true;
	for (int x = 0; x < gridWidth; x++)
	{
		for (int y = 0; y < gridHeight; y++)
		{
			if (board[x, y] == CELL_STATE.COVERED_CELL || board[x, y] == CELL_STATE.FLAGGED_CELL) hasWon = false;
		}
	}

	if (hasWon) gameState = GAME_STATE.WIN;
}

void LeftClick()
{

	if (mouseCellPosX >= 0 && mouseCellPosX < gridWidth && mouseCellPosY >= 0 && mouseCellPosY < gridHeight)
	{
		if (firstClick)
		{
			for (int xo = -1; xo <= 1; xo++)
			{
				for (int yo = -1; yo <= 1; yo++)
				{
					if (mouseCellPosX + xo < 0 || mouseCellPosX + xo >= gridWidth || mouseCellPosY + yo < 0 || mouseCellPosY + yo >= gridHeight) continue;

					board[mouseCellPosX + xo, mouseCellPosY + yo] = CELL_STATE.COVERED_CELL;
				}
			}

			firstClick = false;
		}

		switch (board[mouseCellPosX, mouseCellPosY])
		{
			case CELL_STATE.COVERED_CELL:

				FloodFillClear(mouseCellPosX, mouseCellPosY);
				CheckWin();
				break;
			case CELL_STATE.COVERED_BOMB:
				RevealAllBombs();
				gameState = GAME_STATE.WIN;
				break;
			default:
				break;
		}
	}
}

void RightClick()
{
	if (firstClick) return;

	if (mouseCellPosX >= 0 && mouseCellPosX < gridWidth && mouseCellPosY >= 0 && mouseCellPosY < gridHeight)
	{
		switch (board[mouseCellPosX, mouseCellPosY])
		{
			case CELL_STATE.COVERED_CELL:
				board[mouseCellPosX, mouseCellPosY] = CELL_STATE.FLAGGED_CELL;
				break;
			case CELL_STATE.FLAGGED_CELL:
				board[mouseCellPosX, mouseCellPosY] = CELL_STATE.COVERED_CELL;
				AddBoxParticle(new Vector2(canvasOffsetX + ((float)mouseCellPosX + 0.5f) * cellSize, canvasOffsetY + ((float)mouseCellPosY + 0.5f) * cellSize), Color.White, true);
				break;
			case CELL_STATE.COVERED_BOMB:
				board[mouseCellPosX, mouseCellPosY] = CELL_STATE.FLAGGED_BOMB;
				break;
			case CELL_STATE.FLAGGED_BOMB:
				board[mouseCellPosX, mouseCellPosY] = CELL_STATE.COVERED_BOMB;
				AddBoxParticle(new Vector2(canvasOffsetX + ((float)mouseCellPosX + 0.5f) * cellSize, canvasOffsetY + ((float)mouseCellPosY + 0.5f) * cellSize), Color.White, true);
				break;
			default:
				break;
		}
	}
}

void DrawCellHover()
{
	if (gameState != GAME_STATE.PLAYING) return;
	if (mouseCellPosX >= 0 && mouseCellPosX < gridWidth && mouseCellPosY >= 0 && mouseCellPosY < gridHeight)
	{
		Raylib.DrawRectangleV(new Vector2(canvasOffsetX + mouseCellPosX * cellSize, canvasOffsetY + mouseCellPosY * cellSize), Vector2.One * cellSize, new Color(255, 255, 255, (int)((MathF.Sin((float)Raylib.GetTime() * 4.0f) * 0.5f + 0.6f) * 100.0f)));
	}
}

while (!Raylib.WindowShouldClose())
{

	if (gameState == GAME_STATE.PLAYING)
	{
		mouseCellPosX = (int)MathF.Floor((Raylib.GetMouseX() - canvasOffsetX) / cellSize);
		mouseCellPosY = (int)MathF.Floor((Raylib.GetMouseY() - canvasOffsetY) / cellSize);

		if (Raylib.IsMouseButtonPressed(MouseButton.Left))
		{
			LeftClick();
		}

		if (Raylib.IsMouseButtonPressed(MouseButton.Right))
		{
			RightClick();
		}
	}

	Raylib.BeginDrawing();

	Raylib.ClearBackground(Color.Black);

	DrawBanner();

	DrawBackground();

	DrawBoard();

	DrawCellHover();

	DrawBoxParticles();

	Raylib.DrawFPS(10, 10);

	Raylib.EndDrawing();
}

Raylib.UnloadTexture(topPanelTex);
Raylib.UnloadTexture(bottomPanelTex);
Raylib.UnloadTexture(numbersTex);
Raylib.UnloadTexture(flagTex);
Raylib.UnloadTexture(bombTex);
Raylib.UnloadFont(font);

Raylib.CloseWindow();

enum CELL_STATE
{
	COVERED_CELL = 0,
	COVERED_BOMB = 1,
	UNCOVERED_CELL = 2,
	UNCOVERED_BOMB = 3,
	FLAGGED_CELL = 4,
	FLAGGED_BOMB = 5
}

enum GAME_STATE
{
	PLAYING = 0,
	WIN = 1,
	DEAD = 2
}

public class ParticleBox
{
	public Vector2 position;
	public Vector2 velocity;
	public float angularVel;
	public float rot;
	public Color col;
	public bool isFlag;
};
