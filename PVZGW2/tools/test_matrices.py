"""
Fix: Camera forward is ROW 2 of camera world matrix (for row-major storage).
The +0x320 camera world matrix has forward = [-0.1525, 0.2062, -0.9665]
"""

def main():
    cam_pos = (301.5108, 68.8023, -157.1544)
    
    # Camera world matrix at +0x320 (row-major storage)
    CAM_WORLD = [
        [-0.9878, 0.0000, 0.1559, 0.0000],  # Right axis
        [ 0.0322, 0.9785, 0.2037, 0.0000],  # Up axis
        [-0.1525, 0.2062,-0.9665, 0.0000],  # FORWARD axis
        [301.5108, 68.8023,-157.1544, 1.0000]  # Translation
    ]
    
    # Camera forward is ROW 2 of rotation
    fwd = [CAM_WORLD[2][0], CAM_WORLD[2][1], CAM_WORLD[2][2]]
    print(f"Camera forward: [{fwd[0]:.4f}, {fwd[1]:.4f}, {fwd[2]:.4f}]")
    
    # VIEW matrix at +0x2C0 (with correct -R^T*eye translation)
    VIEW_mem = [
        [-0.9878, 0.0322, -0.1525, 0.0000],
        [ 0.0000, 0.9785,  0.2062, 0.0000],
        [ 0.1559, 0.2037, -0.9665, 0.0000],
        [322.3242, -45.0024, -120.0914, 1.0000]
    ]
    
    # PROJ at +0x3E0
    PROJ_mem = [
        [1.0711, 0.0000, 0.0000, 0.0000],
        [0.0000, 1.9042, 0.0000, 0.0000],
        [0.0000, 0.0000, 0.0003, 0.1000],
        [0.0000, 0.0000,-1.0000, 0.0000]
    ]
    
    # Column-major interpretation: transpose for math
    VIEW = [[VIEW_mem[j][i] for j in range(4)] for i in range(4)]
    PROJ = [[PROJ_mem[j][i] for j in range(4)] for i in range(4)]
    
    # VP = PROJ * VIEW
    def mat_mul(a, b):
        return [[sum(a[i][k] * b[k][j] for k in range(4)) for j in range(4)] for i in range(4)]
    
    VP = mat_mul(PROJ, VIEW)
    
    # Column-vector: clip = VP * pos
    def mat_vec(mat, vec):
        return [sum(mat[i][j] * vec[j] for j in range(4)) for i in range(4)]
    
    # Test points using CORRECT forward vector
    tests = [
        ("camera", cam_pos),
        ("5m fwd", (cam_pos[0]+fwd[0]*5, cam_pos[1]+fwd[1]*5, cam_pos[2]+fwd[2]*5)),
        ("10m fwd", (cam_pos[0]+fwd[0]*10, cam_pos[1]+fwd[1]*10, cam_pos[2]+fwd[2]*10)),
        ("20m fwd", (cam_pos[0]+fwd[0]*20, cam_pos[1]+fwd[1]*20, cam_pos[2]+fwd[2]*20)),
        ("5m right", (cam_pos[0]-0.9878*5, cam_pos[1]+0.0322*5, cam_pos[2]-0.1525*5)),  # Right is row 0
        ("5m up", (cam_pos[0]+0*5, cam_pos[1]+0.9785*5, cam_pos[2]+0.2062*5)),  # Up is row 1
    ]
    
    print("\n" + "=" * 60)
    print("Testing with CORRECT forward direction")
    print("=" * 60)
    
    for name, pos in tests:
        pos4 = [pos[0], pos[1], pos[2], 1.0]
        view = mat_vec(VIEW, pos4)
        clip = mat_vec(VP, pos4)
        
        w = clip[3]
        print(f"\n{name}:")
        print(f"  view = [{view[0]:.4f}, {view[1]:.4f}, {view[2]:.4f}, {view[3]:.4f}]")
        print(f"  clip = [{clip[0]:.4f}, {clip[1]:.4f}, {clip[2]:.4f}, {clip[3]:.4f}]")
        
        if abs(w) > 0.001:
            behind = w < 0
            ndc_x = clip[0] / w
            ndc_y = clip[1] / w
            
            # NDC [-1,1] to screen [0,W]
            sx = (ndc_x + 1) * 0.5 * 1920
            sy = (1 - (ndc_y + 1) * 0.5) * 1080  # Y flip for GL convention
            
            status = "BEHIND" if behind else "OK"
            vis = "VISIBLE" if not behind and 0 <= sx <= 1920 and 0 <= sy <= 1080 else ""
            print(f"  screen = ({sx:.1f}, {sy:.1f}) [{status}] {vis}")
        else:
            print(f"  w ≈ 0")
    
    # Also try without Y flip (DX convention)
    print("\n" + "=" * 60)
    print("Without Y flip (DX convention):")
    print("=" * 60)
    for name, pos in tests:
        pos4 = [pos[0], pos[1], pos[2], 1.0]
        clip = mat_vec(VP, pos4)
        w = clip[3]
        if abs(w) > 0.001:
            ndc_x = clip[0] / w
            ndc_y = clip[1] / w
            sx = (ndc_x + 1) * 0.5 * 1920
            sy = (ndc_y + 1) * 0.5 * 1080  # No flip
            status = "BEHIND" if w < 0 else "OK"
            vis = "VISIBLE" if w > 0 and 0 <= sx <= 1920 and 0 <= sy <= 1080 else ""
            print(f"  {name}: ({sx:.1f}, {sy:.1f}) [{status}] {vis}")
        else:
            print(f"  {name}: w≈0")

if __name__ == '__main__':
    main()
