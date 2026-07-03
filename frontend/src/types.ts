export interface BoardGame {
  id: number;
  name: string;
  minPlayers: number;
  maxPlayers: number;
  minRuntime: number;
  maxRuntime: number;
  minAge: number;
  imageUrl: string | null;
  description: string;
  type: string;
  category: string;
  bggRank: number;
  isOwned: boolean;
}

export interface GameFilter {
  playerCount?: number;
  maxAge?: number;
  type?: string;
  categories?: string[];
  maxRuntime?: number;
}
