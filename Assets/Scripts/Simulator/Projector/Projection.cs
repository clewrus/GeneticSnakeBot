

namespace Simulator {
	public struct Projection {
		public int eyeQuality;

		public float[] food;
		public float[] wall;
		public float[] snake;
		public float[] kinship;

		public Projection (int eyeQuality) {
			this.eyeQuality = eyeQuality;

			food = new float[eyeQuality];
			wall = new float[eyeQuality];
			snake = new float[eyeQuality];
			kinship = new float[eyeQuality];
		}
	}
}