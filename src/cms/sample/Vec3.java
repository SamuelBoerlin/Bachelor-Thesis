package cms.sample;


public class Vec3 {
	public static final Vec3 ONE = new Vec3(1, 1, 1);
	public static final Vec3 ZERO = new Vec3(0, 0, 0);
	
	public final double x, y, z;
	
	public Vec3(double x, double y, double z) {
		this.x = x;
		this.y = y;
		this.z = z;
	}
	
	public double dot(Vec3 vec) {
		return this.x * vec.x + this.y * vec.y + this.z * vec.z;
	}
	
	public Vec3 cross(Vec3 vec) {
		return new Vec3(this.y * vec.z - this.z * vec.y, this.z * vec.x - this.x * vec.z, this.x * vec.y - this.y * vec.x);
	}
	
	public Vec3 mul(double scalar) {
		return new Vec3(this.x * scalar, this.y * scalar, this.z * scalar);
	}
	
	public Vec3 add(Vec3 vec) {
		return new Vec3(this.x + vec.x, this.y + vec.y, this.z + vec.z);
	}
	
	public Vec3 sub(Vec3 vec) {
		return new Vec3(this.x - vec.x, this.y - vec.y, this.z - vec.z);
	}
	
	public double lengthsq() {
		return this.x * this.x + this.y * this.y + this.z * this.z;
	}
	
	public double length() {
		return Math.sqrt(this.x * this.x + this.y * this.y + this.z * this.z);
	}
	
	public Vec3 normalized() {
		return this.mul(1.0D / this.length());
	}
	
	public double distance(Vec3 vec) {
		return Math.sqrt((this.x - vec.x)*(this.x - vec.x) + (this.y - vec.y)*(this.y - vec.y) + (this.z - vec.z)*(this.z - vec.z));
	}
	
	public double distanceSq(Vec3 vec) {
		return (this.x - vec.x)*(this.x - vec.x) + (this.y - vec.y)*(this.y - vec.y) + (this.z - vec.z)*(this.z - vec.z);
	}
	
	@Override
	public String toString() {
		return "(" + this.x + ", " + this.y + ", " + this.z + ")";
	}
}
