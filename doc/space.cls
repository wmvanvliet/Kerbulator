\NeedsTeXFormat{LaTeX2e}
\ProvidesClass{rodin}[2013/10/04 LaTeX class implementing a custom theme]

% This class is based on the article class
\LoadClassWithOptions{article}

\RequirePackage{amsmath}
\RequirePackage{amssymb}
\RequirePackage[pdftex]{graphicx}
\RequirePackage{color} 
\RequirePackage[nice]{nicefrac}
\RequirePackage[tight]{units}
\RequirePackage[round]{natbib}

\RequirePackage{framed}
\setlength{\OuterFrameSep}{1ex}

\RequirePackage[labelfont={bf}, margin=1.5cm]{caption}
\RequirePackage[pdftex, bookmarks, colorlinks, breaklinks]{hyperref}
\definecolor{darkblue}{rgb}{0,0,.4}
\hypersetup{
    linkcolor=darkblue,
    citecolor=darkblue,
    filecolor=black,
    urlcolor=black
}
\makeatletter
\AtBeginDocument{
    \hypersetup{
        pdftitle = {\@title},
        pdfauthor = {\@author}
    }
}
\makeatother
\RequirePackage[parfill]{parskip}
\RequirePackage{url}
\urlstyle{sf}

%\RequirePackage{fontspec}
%\setmainfont{Arial}
%\setsansfont{Arial}
%\setmonofont{Consolas}
%\usepackage[scaled]{uarial}
%\renewcommand{\familydefault}{\sfdefault}
\usepackage[T1]{fontenc}
\usepackage{tgtermes}
%\usepackage[T1]{fontenc}
%\usepackage{MinionPro}

%% Increase line spacing a bit
\usepackage{setspace}
\onehalfspacing

%% Text layout
\topmargin -1.0cm
\oddsidemargin 0.5cm
\evensidemargin 0.5cm
\textwidth 16cm 
\textheight 21cm

\newcommand{\mat}[1]{\mathbf{#1}}
\renewcommand{\vec}[1]{\mathbf{#1}}
\newcommand{\tmat}[1]{$\mathbf{#1}$}
\newcommand{\tvec}[1]{{\bf #1}}

% Emulate smallcaps for acronyms
\RequirePackage[letterspace=45]{microtype}
\newcommand{\ac}[1]{{\fontsize{.694em}{1em}\selectfont\textls{#1}}}

% TO-DO boxes
\definecolor{todo-background}{rgb}{1,.8,.8}
\definecolor{todo-foreground}{rgb}{1,0,0}
\newcommand{\todo}[1]{

    \colorbox{todo-background}{\color{todo-foreground}TODO:\ #1}

}

\makeatletter
\def\maketitle{\begin{center}\LARGE\bf{\@title}\end{center}}
\makeatother

% Create KU Leuven header
\RequirePackage{fancyhdr}
\pagestyle{fancy}

\lhead{}
\chead{}

\makeatletter
\AtBeginDocument {
    \rhead{
		\small
		\flushright
		{\bf {\@title}} \\
		{\@author} \\
		\today 
		\vfill
    }
}
\makeatother
\renewcommand{\headrulewidth}{0pt}
\setlength\headheight{25mm}

\lfoot{}
\cfoot{}

\RequirePackage{lastpage}
\rfoot{page \thepage\ of \pageref*{LastPage}}

\bibliographystyle{plain}
