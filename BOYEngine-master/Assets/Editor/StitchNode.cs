using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Sprites;

public class StitchNode : Editor {

	public NodeEditor masterWindow;

	public int id;
	public Stitch stitch;
	public Rect rect;
	public delegate void voidFunction(int id);
	public voidFunction closeFunction;

	Texture2D bgGreen;
	Texture2D bgRed;
	float minWinHeight = 120;
	float adjustedWinHeight;

	public void DrawGUI(int winID)
	{

		// Resize the window as needed to fit in the yarns
		adjustedWinHeight = 60 + 18 * stitch.yarns.Length + 25;
		rect.height = Mathf.Max (minWinHeight, adjustedWinHeight);

		// Color Background
		bgGreen = (Texture2D)Resources.Load ("green", typeof(Texture2D)) as Texture2D;
		bgRed = (Texture2D)Resources.Load ("red", typeof(Texture2D)) as Texture2D;
		if (stitch.status == Stitch.stitchStatus.start) {
			GUI.DrawTexture (new Rect (0, 16, rect.width, rect.height), bgGreen);
		} else if (stitch.status == Stitch.stitchStatus.end) {
			GUI.DrawTexture (new Rect (0, 16, rect.width, rect.height), bgRed);
		}

			
		GUILayout.TextArea (stitch.summary);

		// Right alignment style for the yarn labels
		GUIStyle rAlign = new GUIStyle (GUI.skin.label);
		rAlign.alignment = TextAnchor.MiddleRight;

		// Label each yarn coming out of this stitch
		GUILayout.BeginArea (new Rect (0, minWinHeight/2, rect.width, rect.height - minWinHeight/2));
		foreach(Yarn y in stitch.yarns) {
			GUILayout.Label(y.choiceString, rAlign);
		}
		GUILayout.EndArea ();
	
		// Close window button
		Color tempClose = GUI.backgroundColor;
		GUI.backgroundColor = Color.red;
		if(GUI.Button(new Rect(rect.width-18,-1,18,18),"X")) {

			masterWindow.RemoveNode (stitch.stitchID);

		}
		GUI.backgroundColor = tempClose;

		// Edit button
		if(GUI.Button(new Rect(rect.width/2 - 20, rect.height -20, 40, 15), "Edit")) {
			NodeEditor.editStitch = id;
		}


		// Setup Style
		GUIStyle yarnButtonStyle = new GUIStyle (GUI.skin.button);
		yarnButtonStyle.padding = new RectOffset ();
		yarnButtonStyle.alignment = TextAnchor.MiddleCenter;

		float buttonSize = 15;

		Color temp = GUI.backgroundColor;
		if (NodeEditor.attachingYarnID == id) {
			GUI.backgroundColor = Color.green;
		}
		if (GUI.Button(new Rect(rect.width - 5 - buttonSize, rect.height -20, buttonSize, buttonSize), "+", yarnButtonStyle)) {
			masterWindow.BeginAttachment(id);
		}
		GUI.backgroundColor = temp;

		if (GUI.Button(new Rect(5, rect.height -20, buttonSize, buttonSize), "=", yarnButtonStyle)) {
			masterWindow.EndAttachment(id);
		}
			


		GUI.DragWindow();

	}


//	public void AttachComplete(Stitch winID)
//	{
//		stitch.yarns.Add(winID);
//	}

}
