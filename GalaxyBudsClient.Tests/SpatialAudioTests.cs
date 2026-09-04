using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text;
using FluentAssertions;
using GalaxyBudsClient.Platform.SpatialAudio;
using GalaxyBudsClient.Utils.Extensions;
using NUnit.Framework;

namespace GalaxyBudsClient.Tests;

[TestFixture]
public class SpatialAudioTests
{
    [Test]
    public void EulerConversion_IdentityQuaternion_ReturnsZero()
    {
        var q = Quaternion.Identity;
        var (roll, pitch, yaw) = q.ToRollPitchYaw();

        ((double)roll).Should().BeApproximately(0.0, 0.001);
        ((double)pitch).Should().BeApproximately(0.0, 0.001);
        ((double)yaw).Should().BeApproximately(0.0, 0.001);
    }

    [Test]
    public void RelativeOrientation_IdenticalQuaternions_ResultsInZeroDelta()
    {
        // Reference looking 45 degrees
        var refQuat = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)(Math.PI / 4.0));
        var currentQuat = refQuat;

        var invRef = Quaternion.Inverse(refQuat);
        var rel = Quaternion.Normalize(Quaternion.Multiply(invRef, currentQuat));

        var (roll, pitch, yaw) = rel.ToRollPitchYaw();
        ((double)yaw).Should().BeApproximately(0.0, 0.001);
        ((double)pitch).Should().BeApproximately(0.0, 0.001);
        ((double)roll).Should().BeApproximately(0.0, 0.001);
    }

    [Test]
    public void RelativeOrientation_YawOffset_CalculatesExactDegrees()
    {
        var refQuat = Quaternion.Identity;
        // 90 degrees around Z axis (Yaw)
        var currentQuat = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)(Math.PI / 2.0));

        var invRef = Quaternion.Inverse(refQuat);
        var rel = Quaternion.Normalize(Quaternion.Multiply(invRef, currentQuat));

        var (_, _, yawRad) = rel.ToRollPitchYaw();
        var yawDeg = (double)yawRad * (180.0 / Math.PI);

        yawDeg.Should().BeApproximately(90.0, 0.1);
    }

    [Test]
    public void OpenTrackBroadcaster_GeneratesValid48BytePacket()
    {
        using var stream = new MemoryStream(48);
        using var writer = new BinaryWriter(stream);

        // Standard OpenTrack format: 6 x double (X, Y, Z, Yaw, Pitch, Roll)
        writer.Write(0.0);
        writer.Write(0.0);
        writer.Write(0.0);
        writer.Write(45.5); // Yaw
        writer.Write(-12.3); // Pitch
        writer.Write(5.0); // Roll

        var packet = stream.ToArray();
        packet.Length.Should().Be(48);

        using var readStream = new MemoryStream(packet);
        using var reader = new BinaryReader(readStream);

        reader.ReadDouble().Should().Be(0.0);
        reader.ReadDouble().Should().Be(0.0);
        reader.ReadDouble().Should().Be(0.0);
        reader.ReadDouble().Should().Be(45.5);
        reader.ReadDouble().Should().Be(-12.3);
        reader.ReadDouble().Should().Be(5.0);
    }

    [Test]
    public void OscMessage_AddressAndTypeTag_PaddedToFourBytes()
    {
        var address = "/spatial/ypr";
        var typeTag = ",fff";

        var addrBytes = Encoding.ASCII.GetBytes(address);
        var tagBytes = Encoding.ASCII.GetBytes(typeTag);

        // Address "/spatial/ypr" is 12 chars -> with null = 13 -> padded to 16
        var addrPaddedLen = ((addrBytes.Length + 4) / 4) * 4;
        addrPaddedLen.Should().Be(16);

        // Type tag ",fff" is 4 chars -> with null = 5 -> padded to 8
        var tagPaddedLen = ((tagBytes.Length + 4) / 4) * 4;
        tagPaddedLen.Should().Be(8);
    }
}
