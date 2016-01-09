using UnityEngine;
using System.Collections;

public static class Ext_Color_HSV {
	public static Color ToHSV(this Color rgb){
		Color temp = new Color();
		
		float M = 0f, m = 0f, c = 0f;
		
		float[] RGB = {rgb.r, rgb.g, rgb.b};
		M = Mathf.Max(RGB);
		m = Mathf.Min(RGB);
		c = M - m;
		temp.b = M;
		if (c != 0f)
		{
			if (M == rgb.r){
				temp.r = ((rgb.g - rgb.b) / c) % 6f;
			} else if (M == rgb.g){
				temp.r = (rgb.b - rgb.r) / c + 2f;
			} else {/*if(M==b)*/
				temp.r = (rgb.r - rgb.g) / c + 4f;
			}
			temp.r *= 60f;
			temp.g = c / temp.b;
		}
		temp.a = rgb.a;
		return temp;
	}
	public static Color ToRGB(this Color hsv){
		Color temp;
		float c = 0f, m = 0f, x = 0f;
		c = hsv.b * hsv.g;
		x = c * (1f - Mathf.Abs(((hsv.r / 60f) % 2f) - 1f));
		m = hsv.b - c;
		if (hsv.r >= 0f && hsv.r < 60f)
		{
			temp = new Color(c + m, x + m, m);
		}
		else if (hsv.r >= 60f && hsv.r < 120f)
		{
			temp = new Color(x + m, c + m, m);
		}
		else if (hsv.r >= 120f && hsv.r < 180f)
		{
			temp = new Color(m, c + m, x + m);
		}
		else if (hsv.r >= 180f && hsv.r < 240f)
		{
			temp = new Color(m, x + m, c + m);
		}
		else if (hsv.r >= 240f && hsv.r < 300f)
		{
			temp = new Color(x + m, m, c + m);
		}
		else if (hsv.r >= 300f && hsv.r < 360f)
		{
			temp = new Color(c + m, m, x + m);
		}
		else
		{
			temp = new Color(m, m, m);
		}
		temp.a = hsv.a;
		return temp;
	}
}
