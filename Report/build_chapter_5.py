#!/usr/bin/env python3
"""Chapter 5: at most two pages, paragraphs only, no invented buyer results."""

from docx import Document
from docx.shared import Pt, Cm, RGBColor
from docx.enum.text import WD_LINE_SPACING, WD_ALIGN_PARAGRAPH
from docx.oxml.ns import qn

OUT = "/Users/cikirezi/Library/CloudStorage/OneDrive-ThePartnershipAfrica/Documents/GitHub/CP/Report/Mwiza_Cannelle_Capstone_Report_Chapter_5.docx"


def set_run(run, size=12, bold=False, italic=False):
    run.font.name = "Times New Roman"
    run._element.rPr.rFonts.set(qn("w:eastAsia"), "Times New Roman")
    run.font.size = Pt(size)
    run.bold = bold
    run.italic = italic
    run.font.color.rgb = RGBColor(0, 0, 0)


def p(doc, text, *, size=12, bold=False, italic=False, center=False, space_after=10, first=True):
    para = doc.add_paragraph()
    para.paragraph_format.space_after = Pt(space_after)
    para.paragraph_format.space_before = Pt(0)
    para.paragraph_format.line_spacing_rule = WD_LINE_SPACING.ONE_POINT_FIVE
    if center:
        para.alignment = WD_ALIGN_PARAGRAPH.CENTER
    else:
        para.alignment = WD_ALIGN_PARAGRAPH.JUSTIFY
        if first:
            para.paragraph_format.first_line_indent = Cm(1.25)
    run = para.add_run(text)
    set_run(run, size, bold, italic)
    return para


def build():
    doc = Document()
    for s in doc.sections:
        s.top_margin = Cm(2.54)
        s.bottom_margin = Cm(2.54)
        s.left_margin = Cm(2.54)
        s.right_margin = Cm(2.54)
    doc.styles["Normal"].font.name = "Times New Roman"
    doc.styles["Normal"].font.size = Pt(12)

    p(doc, "CHAPTER FIVE", size=16, bold=True, center=True, first=False, space_after=4)
    p(doc, "Conclusion and Recommendations", size=14, bold=True, center=True, first=False, space_after=8)
    p(doc, "Keep this chapter within two pages. It is paragraphs only. Do not add a score, a buyer count, or a phone result that Chapter 4 does not already contain.", first=False, italic=True, space_after=12)

    p(doc, "This study set out to design, implement, and evaluate an augmented reality prototype that could reduce buyer uncertainty when an adult in Kigali considers a physical artwork online. The uncertainty in view was appearance, scale, spatial fit, suitability, and preference. The prototype was to meet that uncertainty with a preview at the stored size, a reply the researcher could check, a story note kept with the work, and three other works after the buyer kept, saved, or dismissed the one on the wall.")
    p(doc, "What the study completed is that prototype, and the checks on it. A work is stored in centimetres and placed at those centimetres divided by 100, and the editor visit and the phone session use the same conversion. The reply table answers size, colour, and the story note in English, French, and Kinyarwanda. A question outside the table receives one fixed line, and no fact is added. Keep adds 2 to the style, the mood, and the colour, save adds 1, and dismiss subtracts 1. The visit then offers three other works and leaves the current work out. A valid entrance code can write the visit. An invalid code writes nothing. On 13 November 2026, 24 EditMode tests passed and none failed. The editor visit showed the same path on one screen, from the piece, to a known reply, to a refused reply, to the three works after keep.")
    p(doc, "The conclusion is drawn only from that evidence. The second research question is answered. One visit can carry the preview, the reply, the story note, and the re-ranking, and each of those can be traced to the stored record and to the buyer’s decision. The first research question is answered only as far as the requirements taken from the approved proposal. The interviews that were to hear the factors from buyers in Kigali were not conducted. The third research question is not answered. The comparison with 25 buyers was not conducted, so this report does not claim a change in buyer uncertainty, a usability score, or a gain in decision confidence. The aim of the study is met in its design and in its implementation. It is not met in its evaluation with buyers.")
    p(doc, "The recommendation that follows is to run the evaluation Chapter 3 already designed, on this prototype, with a static page as the comparison and the five-item uncertainty score as the primary measure. Decision confidence, recommendation relevance, usability, satisfaction, and contextual understanding stay supporting readings of that score, as the proposal set them. Until those sessions exist, the claim this report can stand on is the one above. A later catalogue can hold works consented by artists in Kigali, and the story notes can be read back to those artists, without replacing the size rule or the reply table. The table should remain written. A sentence composed at the moment of the question could state a size, a colour, or a meaning the record does not hold, and the visit would then be harder to defend.")
    p(doc, "The contribution of the work is a small decision-support visit for a physical artwork. It was built for the Kigali setting described in the proposal, and it was checked against its own rules. It gives a buyer a size, a reply, a note, and a next work, and it gives a reviewer a way to see where each of those came from. It is a basis for the buyer study. It is not a substitute for that study.")

    doc.save(OUT)
    print(OUT)


if __name__ == "__main__":
    build()
