﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RetroSpy.Readers
{
    static public class SuperNESandNES
    {
        static ControllerState readPacketButtons(byte[] packet, string[] buttons)
        {
            if (packet.Length < buttons.Length) return null;

            var state = new ControllerStateBuilder();

            for (int i = 0; i < buttons.Length; ++i) {
                if (string.IsNullOrEmpty(buttons[i])) continue;
                state.SetButton(buttons[i], packet[i] != 0x00);
            }

            return state.Build();
        }


        static ControllerState readPacketButtons_ascii(byte[] packet, string[] buttons)
        {
            if (packet.Length < buttons.Length) return null;

            var state = new ControllerStateBuilder();

            for (int i = 0; i < buttons.Length; ++i)
            {
                if (string.IsNullOrEmpty(buttons[i])) continue;
                state.SetButton(buttons[i], packet[i] != '0');
            }

            return state.Build();
        }
        static readonly string[] BUTTONS_NES = {
            "a", "b", "select", "start", "up", "down", "left", "right"
        };

        static readonly string[] BUTTONS_SNES = {
            "b", "y", "select", "start", "up", "down", "left", "right", "a", "x", "l", "r", null, null, null, null
        };

        static readonly string[] BUTTONS_INTELLIVISION = {
            "n", "nne", "ne", "ene", "e", "ese", "se", "sse", "s", "ssw", "sw", "wsw", "w", "wnw", "nw", "nnw", "1", "2", "3", "4", "5", "6", "7", "8", "9", "clear", "0", "enter", "topleft", "topright", "bottomleft", "bottomright"
        };

        static readonly string[] BUTTONS_CD32 =
        {
            null, "blue", "red", "yellow", "green", "forward", "backward", "pause", null
        };

        static readonly string[] BUTTONS_CDTV_REMOTE =
        {
            "mouse_left", "mouse_up", "mouse_right", "mouse_down", "right_button", "left_button", "Num1", "Num2", "Num3", "Num4", "Num5", "Num6", "Num7", "Num8", "Num9", "Num0", "Escape", "Enter", "Genlock", "CDTV", "Power", "Rew", "Play", "FF", "Stop", "VolumeUp", "VolumeDown", "left", "up", "right", "down", "2", "1"
        };

        static readonly string[] BUTTONS_CDTV_JOYSTICK =
        {
            null, "2", "1", "right", "left", "down", "up", "Joy22", "Joy22", "Joy2Right", "Joy2Left", "Joy2Down", "Joy2Up", null, null, null, null, null, null, null, null, null, null, null, null, null
        };

        static readonly string[] BUTTONS_PSCLASSIC =
        {
            "r1", "l1", "r2", "l2", "square", "x", "circle", "triangle", null, null, "down", "up", "right", "left", "start", "select"
        };


        static public ControllerState ReadFromPacket_Intellivision(byte[] packet)
        {
            return readPacketButtons(packet, BUTTONS_INTELLIVISION);
        }

        static public ControllerState ReadFromPacket_NES (byte[] packet) {
            return readPacketButtons(packet, BUTTONS_NES);
        }

        static public ControllerState ReadFromPacket_PSClassic(byte[] packet) {
                return readPacketButtons_ascii(packet, BUTTONS_PSCLASSIC);
        }

        static public ControllerState ReadFromPacket_CD32(byte[] packet)
        {
            ControllerStateBuilder state = null;
            if (packet.Length == 13)
            {
                return Classic.ReadFromPacket(packet);
            }
            if (packet.Length == BUTTONS_CD32.Length)
            {
                state = new ControllerStateBuilder();

                for (int i = 0; i < BUTTONS_CD32.Length; ++i)
                {
                    if (string.IsNullOrEmpty(BUTTONS_CD32[i])) continue;
                    state.SetButton(BUTTONS_CD32[i], (packet[i] & 0b10000000) == 0x00);
                }

                state.SetButton("up", (packet[8] & 0b00000001) == 0);
                state.SetButton("down", (packet[0] & 0b00000100) == 0);
                state.SetButton("left", (packet[0] & 0b00001000) == 0);
                state.SetButton("right", (packet[0]& 0b00010000) == 0);
            }
            else if (packet.Length == BUTTONS_CDTV_REMOTE.Length)
            {
                state = new ControllerStateBuilder();

                for (int i = 0; i < BUTTONS_CDTV_REMOTE.Length; ++i)
                {
                    if (string.IsNullOrEmpty(BUTTONS_CDTV_REMOTE[i])) continue;
                    state.SetButton(BUTTONS_CDTV_REMOTE[i], packet[i] != 0x00);
                }

                float x = 0;
                float y = 0;

                if (packet[0] != 0x00)
                    x = -0.25f;
                else if (packet[2] != 0x00)
                    x = 0.25f;
                if (packet[1] != 0x00)
                    y = 0.25f;
                else if (packet[3] != 0x00)
                    y = -0.25f;

                SignalTool.SetMouseProperties(x, y, state, 0.25f);

            }
            else if (packet.Length == BUTTONS_CDTV_JOYSTICK.Length && packet[0] == 0)
            {
                int checksum = (packet[24] >> 4) | packet[25];
                int checkedCheckSum = 0;
                for (int i = 0; i < 24; ++i)
                    checkedCheckSum += packet[i] == 0 ? 0 : 1;

                if (checksum == checkedCheckSum)
                {
                    state = new ControllerStateBuilder();

                    for (int i = 0; i < BUTTONS_CDTV_JOYSTICK.Length; ++i)
                    {
                        if (string.IsNullOrEmpty(BUTTONS_CDTV_JOYSTICK[i])) continue;
                        state.SetButton(BUTTONS_CDTV_JOYSTICK[i], packet[i] != 0x00);
                    }

                    SignalTool.FakeAnalogStick(packet[6], packet[5], packet[4], packet[3], state, "x", "y");
                    SignalTool.FakeAnalogStick(packet[12], packet[11], packet[10], packet[9], state, "Joy2x", "Joy2y");               
                }
            }
            else if (packet.Length == 26 && packet[0] == 1)
            {
                int checksum = (packet[24] >> 4) | packet[25];
                int checkedCheckSum = 0;
                for (int i = 0; i < 24; ++i)
                    checkedCheckSum += packet[i] == 0 ? 0 : 1;

                if (checksum == checkedCheckSum)
                {
                    state = new ControllerStateBuilder();

                    state.SetButton("left_button", packet[2] == 0x00);
                    state.SetButton("right_button", packet[1] == 0x00);

                    sbyte xVal = (sbyte)SignalTool.readByte(packet, 3);
                    sbyte yVal = (sbyte)SignalTool.readByte(packet, 11);

                    SignalTool.SetMouseProperties(xVal / -128.0f, yVal / 128.0f, state);
                }
            }
            else if (packet.Length == 19)
            {
                state = new ControllerStateBuilder();

                state.SetButton("left_button", packet[0] != 0x00);
                state.SetButton("right_button", packet[2] != 0x00);

                sbyte xVal = (sbyte)SignalTool.readByteBackwards(packet, 3);
                sbyte yVal = (sbyte)SignalTool.readByteBackwards(packet, 11);

                SignalTool.SetMouseProperties( xVal / -128.0f, yVal / 128.0f, state);
            }

            return state != null ? state.Build() : null;
        }

        static public ControllerState ReadFromPacket_SNES (byte[] packet) {
            if (packet.Length < BUTTONS_SNES.Length) return null;

            var state = new ControllerStateBuilder();

            for (int i = 0; i < BUTTONS_SNES.Length; ++i)
            {
                if (string.IsNullOrEmpty(BUTTONS_SNES[i])) continue;
                state.SetButton(BUTTONS_SNES[i], packet[i] != 0x00);
            }

            if (state != null && packet.Length == 32 && packet[15] != 0x00)
            {
                float y = (float)(SignalTool.readByte(packet, 17, 7, 0x1) * ((packet[16] & 0x1) != 0 ? 1 : -1)) / 127;
                float x = (float)(SignalTool.readByte(packet, 25, 7, 0x1) * ((packet[24] & 0x1) != 0 ? -1 : 1)) / 127;
                SignalTool.SetMouseProperties(x, y, state);

            }

            return state.Build();
        }

        static public ControllerState ReadFromPacket_Jaguar (byte[] packet)
        {
            if (packet.Length < 4) return null;

            var state = new ControllerStateBuilder();

            state.SetButton("pause",    (packet[0] & 0b00000100) == 0x00);
            state.SetButton("a",        (packet[0] & 0b00001000) == 0x00);
            state.SetButton("right",    (packet[0] & 0b00010000) == 0x00);
            state.SetButton("left",     (packet[0] & 0b00100000) == 0x00);
            state.SetButton("down",     (packet[0] & 0b01000000) == 0x00);
            state.SetButton("up",       (packet[0] & 0b10000000) == 0x00);

            state.SetButton("b",        (packet[1] & 0b00001000) == 0x00);
            state.SetButton("1",        (packet[1] & 0b00010000) == 0x00);
            state.SetButton("4",        (packet[1] & 0b00100000) == 0x00);
            state.SetButton("l",        (packet[1] & 0b00100000) == 0x00);
            state.SetButton("7",        (packet[1] & 0b01000000) == 0x00);
            state.SetButton("x",        (packet[1] & 0b01000000) == 0x00);
            state.SetButton("star",     (packet[1] & 0b10000000) == 0x00);

            state.SetButton("c", (packet[2] & 0b00001000) == 0x00);
            state.SetButton("2", (packet[2] & 0b00010000) == 0x00);
            state.SetButton("5", (packet[2] & 0b00100000) == 0x00);
            state.SetButton("8", (packet[2] & 0b01000000) == 0x00);
            state.SetButton("y", (packet[2] & 0b01000000) == 0x00);
            state.SetButton("0", (packet[2] & 0b10000000) == 0x00);

            state.SetButton("option",   (packet[3] & 0b00001000) == 0x00);
            state.SetButton("3",        (packet[3] & 0b00010000) == 0x00);
            state.SetButton("6",        (packet[3] & 0b00100000) == 0x00);
            state.SetButton("r",        (packet[3] & 0b00100000) == 0x00);
            state.SetButton("9",        (packet[3] & 0b01000000) == 0x00);
            state.SetButton("z",        (packet[3] & 0b01000000) == 0x00);
            state.SetButton("pound",    (packet[3] & 0b10000000) == 0x00);

            return state.Build();

        }

    }
}
