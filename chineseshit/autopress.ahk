autopress := False

SetTimer teleport, 200
return

F::
{
	autopress := Not(autopress)
	Return
}

teleport:
{
	if (autopress)
	{
		Send, {y}
	}
	Return
}

F10::Reload